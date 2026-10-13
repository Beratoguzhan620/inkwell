using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Abstract;
using BlogApp.Application.Text;
using BlogApp.Core.Results;
using BlogApp.Data;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BlogApp.Application.Services.Concrete;

public class BlogPostService(AppDbContext context, TimeProvider timeProvider) : IBlogPostService
{
    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<BlogPostDto>> GetPublishedAsync(PostListQuery query)
    {
        var posts = context.BlogPosts
            .AsNoTracking()
            .Where(p => p.Status == BlogPostStatus.Approved);

        if (query.CategoryId is not null)
            posts = posts.Where(p => p.CategoryId == query.CategoryId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            posts = posts.Where(p => p.Title.Contains(term) || p.Content.Contains(term));
        }

        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var totalCount = await posts.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);

        var rows = await posts
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ListRow)
            .ToListAsync();

        return new PagedResult<BlogPostDto>
        {
            Items = rows.Select(r => WithReadingTime(r.Post, r.Content)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<Result<BlogPostDetailDto>> GetBySlugAsync(string slug, int? viewerId)
    {
        var post = await context.BlogPosts
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Author)
            .Include(p => p.Comments.OrderBy(c => c.CreatedAt))
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Slug == slug);

        var isOwner = post is not null && viewerId == post.AuthorId;

        if (post is null || (post.Status != BlogPostStatus.Approved && !isOwner))
            return Result<BlogPostDetailDto>.Fail("Yazı bulunamadı.");

        var authorPublishedCount = await context.BlogPosts
            .CountAsync(p => p.AuthorId == post.AuthorId && p.Status == BlogPostStatus.Approved);

        var dto = new BlogPostDetailDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Excerpt = post.Excerpt,
            CoverImageUrl = post.CoverImageUrl,
            CategoryId = post.CategoryId,
            CategoryName = post.Category.Name,
            AuthorId = post.AuthorId,
            AuthorFullName = $"{post.Author.FirstName} {post.Author.LastName}",
            Status = post.Status,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            PublishedAt = post.PublishedAt,
            ReadingMinutes = MarkdownRenderer.ReadingMinutes(post.Content),
            CommentCount = post.Comments.Count,
            ContentHtml = MarkdownRenderer.ToHtml(post.Content),
            AuthorPublishedCount = authorPublishedCount,
            IsOwnedByViewer = isOwner,
            Comments = post.Comments.Select(c => new CommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserFullName = $"{c.User.FirstName} {c.User.LastName}",
                Content = c.Content,
                CreatedAt = c.CreatedAt,
            }).ToList(),
        };

        return Result<BlogPostDetailDto>.Ok(dto);
    }

    public Task<string?> GetSlugAsync(int postId) =>
        context.BlogPosts.Where(p => p.Id == postId).Select(p => p.Slug).FirstOrDefaultAsync();

    public async Task<List<BlogPostDto>> GetAuthorPostsAsync(int authorId)
    {
        var rows = await context.BlogPosts
            .AsNoTracking()
            .Where(p => p.AuthorId == authorId)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Select(ListRow)
            .ToListAsync();

        return rows.Select(r => WithReadingTime(r.Post, r.Content)).ToList();
    }

    public async Task<Result<BlogPostEditDto>> GetForEditAsync(int postId, int authorId)
    {
        var post = await context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId);

        if (post is null || post.AuthorId != authorId)
            return Result<BlogPostEditDto>.Fail("Yazı bulunamadı.");

        return Result<BlogPostEditDto>.Ok(new BlogPostEditDto
        {
            Id = post.Id,
            CategoryId = post.CategoryId,
            Title = post.Title,
            Content = post.Content,
            CoverImageUrl = post.CoverImageUrl,
            IsPublished = post.Status == BlogPostStatus.Approved,
            Slug = post.Slug,
        });
    }

    public async Task<Result<string>> CreateAsync(SaveBlogPostRequest request)
    {
        var check = await CheckAuthorAndCategoryAsync(request);
        if (!check.Success)
            return Result<string>.Fail(check.Message);

        var now = UtcNow;
        var post = new BlogPost
        {
            Title = request.Title.Trim(),
            Slug = await CreateUniqueSlugAsync(request.Title),
            Content = request.Content,
            Excerpt = MarkdownRenderer.Excerpt(request.Content),
            CoverImageUrl = request.CoverImageUrl,
            AuthorId = request.AuthorId,
            CategoryId = request.CategoryId,
            CreatedAt = now,
            Status = request.Publish ? BlogPostStatus.Approved : BlogPostStatus.Draft,
            PublishedAt = request.Publish ? now : null,
        };

        context.BlogPosts.Add(post);
        await context.SaveChangesAsync();

        return Result<string>.Ok(post.Slug, request.Publish ? "Yazınız yayınlandı." : "Taslak kaydedildi.");
    }

    public async Task<Result<string>> UpdateAsync(int postId, SaveBlogPostRequest request)
    {
        var post = await context.BlogPosts.FirstOrDefaultAsync(p => p.Id == postId);

        if (post is null || post.AuthorId != request.AuthorId)
            return Result<string>.Fail("Yazı bulunamadı.");

        var check = await CheckAuthorAndCategoryAsync(request);
        if (!check.Success)
            return Result<string>.Fail(check.Message);

        var wasPublished = post.Status == BlogPostStatus.Approved;

        // The URL stays stable once a post has been published, so shared links keep working.
        if (!wasPublished && !string.Equals(post.Title, request.Title.Trim(), StringComparison.Ordinal))
            post.Slug = await CreateUniqueSlugAsync(request.Title, post.Id);

        post.Title = request.Title.Trim();
        post.Content = request.Content;
        post.Excerpt = MarkdownRenderer.Excerpt(request.Content);
        post.CategoryId = request.CategoryId;
        post.UpdatedAt = UtcNow;

        if (request.RemoveCover)
            post.CoverImageUrl = null;
        if (request.CoverImageUrl is not null)
            post.CoverImageUrl = request.CoverImageUrl;

        if (request.Publish && !wasPublished)
        {
            post.Status = BlogPostStatus.Approved;
            post.PublishedAt = UtcNow;
        }
        else if (!request.Publish && wasPublished)
        {
            post.Status = BlogPostStatus.Draft;
        }

        await context.SaveChangesAsync();

        var message = request.Publish
            ? (wasPublished ? "Değişiklikler yayında." : "Yazınız yayınlandı.")
            : (wasPublished ? "Yazı yayından kaldırıldı ve taslağa alındı." : "Taslak kaydedildi.");

        return Result<string>.Ok(post.Slug, message);
    }

    public async Task<Result> DeleteAsync(int postId, int authorId)
    {
        var post = await context.BlogPosts.FirstOrDefaultAsync(p => p.Id == postId);

        if (post is null || post.AuthorId != authorId)
            return Result.Fail("Yazı bulunamadı.");

        context.BlogPosts.Remove(post);
        await context.SaveChangesAsync();

        return Result.Ok("Yazı silindi.");
    }

    private async Task<Result> CheckAuthorAndCategoryAsync(SaveBlogPostRequest request)
    {
        var author = await context.Users.FindAsync(request.AuthorId);
        if (author is null || !author.IsActive)
            return Result.Fail("Yazı yazabilmek için hesabınızın aktif olması gerekir.");

        if (!await context.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return Result.Fail("Geçerli bir kategori seçin.");

        return Result.Ok();
    }

    private async Task<string> CreateUniqueSlugAsync(string title, int? exceptPostId = null)
    {
        var baseSlug = SlugGenerator.Generate(title);
        var slug = baseSlug;
        var suffix = 2;

        while (await context.BlogPosts.AnyAsync(p => p.Slug == slug && p.Id != exceptPostId))
            slug = $"{baseSlug}-{suffix++}";

        return slug;
    }

    private sealed class PostRow
    {
        public BlogPostDto Post { get; init; } = default!;
        public string Content { get; init; } = string.Empty;
    }

    // Kept as an expression so EF Core translates it to SQL (navigations included).
    private static readonly Expression<Func<BlogPost, PostRow>> ListRow = p => new PostRow
    {
        Content = p.Content,
        Post = new BlogPostDto
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            Excerpt = p.Excerpt,
            CoverImageUrl = p.CoverImageUrl,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            AuthorId = p.AuthorId,
            AuthorFullName = p.Author.FirstName + " " + p.Author.LastName,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            PublishedAt = p.PublishedAt,
            CommentCount = p.Comments.Count,
        },
    };

    private static BlogPostDto WithReadingTime(BlogPostDto post, string content)
    {
        post.ReadingMinutes = MarkdownRenderer.ReadingMinutes(content);
        return post;
    }
}
