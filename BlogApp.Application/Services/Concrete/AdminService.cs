using BlogApp.Application.DTOs.Admin;
using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using BlogApp.Data;
using BlogApp.Data.Constants;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Application.Services.Concrete;

public class AdminService(AppDbContext context, TimeProvider timeProvider) : IAdminService
{
    private const int MaxBulkSize = 100;

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        var weekAgo = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);

        return new AdminDashboardDto
        {
            PublishedPosts = await context.BlogPosts.CountAsync(p => p.Status == BlogPostStatus.Approved),
            DraftPosts = await context.BlogPosts.CountAsync(p => p.Status == BlogPostStatus.Draft),
            HiddenPosts = await context.BlogPosts.CountAsync(p => p.Status == BlogPostStatus.Rejected),
            Users = await context.Users.CountAsync(),
            Authors = await context.UserRoles.CountAsync(ur => ur.RoleId == RoleConstants.AuthorId),
            CommentsLast7Days = await context.Comments.CountAsync(c => c.CreatedAt >= weekAgo),
            RecentPosts = await PostRows(context.BlogPosts.OrderByDescending(p => p.CreatedAt)).Take(5).ToListAsync(),
            RecentComments = await CommentRows(context.Comments.OrderByDescending(c => c.CreatedAt)).Take(5).ToListAsync(),
        };
    }

    public Task<PagedResult<AdminPostRowDto>> GetPostsAsync(AdminPostQuery query)
    {
        var posts = context.BlogPosts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            posts = posts.Where(p => p.Title.Contains(term) || (p.Author.FirstName + " " + p.Author.LastName).Contains(term));
        }

        if (query.CategoryId is not null)
            posts = posts.Where(p => p.CategoryId == query.CategoryId);

        posts = query.Status switch
        {
            PostStatusFilter.Published => posts.Where(p => p.Status == BlogPostStatus.Approved),
            PostStatusFilter.Draft => posts.Where(p => p.Status == BlogPostStatus.Draft || p.Status == BlogPostStatus.Pending),
            PostStatusFilter.Hidden => posts.Where(p => p.Status == BlogPostStatus.Rejected),
            _ => posts,
        };

        return PageAsync(PostRows(posts.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)), query);
    }

    public async Task<Result> SetPostHiddenAsync(int postId, bool hidden)
    {
        var post = await context.BlogPosts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null)
            return Result.Fail("Yazı bulunamadı.");

        if (hidden)
        {
            if (post.Status != BlogPostStatus.Approved)
                return Result.Fail("Yalnızca yayındaki yazılar kaldırılabilir.");

            post.Status = BlogPostStatus.Rejected;
        }
        else
        {
            if (post.Status != BlogPostStatus.Rejected)
                return Result.Fail("Bu yazı zaten kaldırılmış değil.");

            post.Status = BlogPostStatus.Approved;
            post.PublishedAt ??= timeProvider.GetUtcNow().UtcDateTime;
        }

        await context.SaveChangesAsync();
        return Result.Ok(hidden ? "Yazı yayından kaldırıldı." : "Yazı yeniden yayında.");
    }

    public async Task<Result> DeletePostAsync(int postId)
    {
        var deleted = await context.BlogPosts.Where(p => p.Id == postId).ExecuteDeleteAsync();
        return deleted == 0 ? Result.Fail("Yazı bulunamadı.") : Result.Ok("Yazı ve yorumları silindi.");
    }

    public Task<PagedResult<AdminUserRowDto>> GetUsersAsync(AdminUserQuery query)
    {
        var users = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            users = users.Where(u => (u.FirstName + " " + u.LastName).Contains(term) || u.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
            users = users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == query.Role));

        var rows = users
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new AdminUserRowDto
            {
                Id = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                Email = u.Email,
                Role = u.UserRoles.OrderBy(ur => ur.RoleId).Select(ur => ur.Role.Name).FirstOrDefault() ?? "",
                IsActive = u.IsActive,
                IsEmailVerified = u.IsEmailVerified,
                PostCount = u.Posts.Count,
            });

        return PageAsync(rows, query);
    }

    public async Task<Result> SetUserRoleAsync(int userId, int roleId, int actingUserId)
    {
        if (!RoleConstants.All.ContainsKey(roleId))
            return Result.Fail("Geçersiz rol.");

        if (userId == actingUserId && roleId != RoleConstants.AdminId)
            return Result.Fail("Kendi yönetici yetkinizi kaldıramazsınız.");

        var user = await context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Result.Fail("Kullanıcı bulunamadı.");

        context.UserRoles.RemoveRange(user.UserRoles);
        context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        await context.SaveChangesAsync();

        return Result.Ok($"{user.FirstName} {user.LastName} artık {RoleLabel(roleId)}. Değişiklik bir sonraki girişinde geçerli olur.");
    }

    public async Task<Result> SetUserActiveAsync(int userId, bool active, int actingUserId)
    {
        if (userId == actingUserId && !active)
            return Result.Fail("Kendi hesabınızı devre dışı bırakamazsınız.");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Result.Fail("Kullanıcı bulunamadı.");

        if (active && !user.IsEmailVerified)
            return Result.Fail("E-postası doğrulanmamış hesap etkinleştirilemez.");

        user.IsActive = active;
        await context.SaveChangesAsync();

        return Result.Ok(active ? "Hesap etkinleştirildi." : "Hesap devre dışı bırakıldı.");
    }

    public Task<PagedResult<AdminCommentRowDto>> GetCommentsAsync(AdminCommentQuery query)
    {
        var comments = context.Comments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            comments = comments.Where(c => c.Content.Contains(term) || c.Post.Title.Contains(term)
                || (c.User.FirstName + " " + c.User.LastName).Contains(term));
        }

        return PageAsync(CommentRows(comments.OrderByDescending(c => c.CreatedAt)), query);
    }

    public async Task<Result> DeleteCommentsAsync(IReadOnlyCollection<int> commentIds)
    {
        if (commentIds.Count == 0)
            return Result.Fail("Önce en az bir yorum seçin.");

        if (commentIds.Count > MaxBulkSize)
            return Result.Fail($"Tek seferde en fazla {MaxBulkSize} yorum silinebilir.");

        var deleted = await context.Comments.Where(c => commentIds.Contains(c.Id)).ExecuteDeleteAsync();
        return Result.Ok($"{deleted} yorum silindi.");
    }

    public async Task<Result> CreateCategoryAsync(string name)
    {
        var check = await CheckCategoryNameAsync(name, null);
        if (!check.Success)
            return check;

        context.Categories.Add(new Category { Name = name.Trim() });
        await context.SaveChangesAsync();
        return Result.Ok("Kategori eklendi.");
    }

    public async Task<Result> RenameCategoryAsync(int categoryId, string name)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null)
            return Result.Fail("Kategori bulunamadı.");

        var check = await CheckCategoryNameAsync(name, categoryId);
        if (!check.Success)
            return check;

        category.Name = name.Trim();
        await context.SaveChangesAsync();
        return Result.Ok("Kategori güncellendi.");
    }

    public async Task<Result> DeleteCategoryAsync(int categoryId)
    {
        if (await context.BlogPosts.AnyAsync(p => p.CategoryId == categoryId))
            return Result.Fail("Bu kategoride yazı olduğu için silinemez.");

        var deleted = await context.Categories.Where(c => c.Id == categoryId).ExecuteDeleteAsync();
        return deleted == 0 ? Result.Fail("Kategori bulunamadı.") : Result.Ok("Kategori silindi.");
    }

    private async Task<Result> CheckCategoryNameAsync(string? name, int? exceptId)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length is < 2 or > 100)
            return Result.Fail("Kategori adı 2 ile 100 karakter arasında olmalıdır.");

        if (await context.Categories.AnyAsync(c => c.Name == trimmed && c.Id != exceptId))
            return Result.Fail("Bu adla bir kategori zaten var.");

        return Result.Ok();
    }

    private static string RoleLabel(int roleId) => roleId switch
    {
        RoleConstants.AdminId => "yönetici",
        RoleConstants.AuthorId => "yazar",
        _ => "okur",
    };

    private static IQueryable<AdminPostRowDto> PostRows(IQueryable<BlogPost> posts) =>
        posts.Select(p => new AdminPostRowDto
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            AuthorName = p.Author.FirstName + " " + p.Author.LastName,
            CategoryName = p.Category.Name,
            Status = p.Status,
            CommentCount = p.Comments.Count,
            CreatedAt = p.CreatedAt,
            PublishedAt = p.PublishedAt,
        });

    private static IQueryable<AdminCommentRowDto> CommentRows(IQueryable<Comment> comments) =>
        comments.Select(c => new AdminCommentRowDto
        {
            Id = c.Id,
            PostId = c.PostId,
            PostTitle = c.Post.Title,
            PostSlug = c.Post.Slug,
            UserName = c.User.FirstName + " " + c.User.LastName,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
        });

    private static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> source, AdminQuery query)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var total = await source.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);

        return new PagedResult<T>
        {
            Items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }
}
