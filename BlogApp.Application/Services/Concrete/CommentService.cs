using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using BlogApp.Data;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Application.Services.Concrete;

public class CommentService(AppDbContext context, TimeProvider timeProvider) : ICommentService
{
    public const int MaxLength = 2000;

    public async Task<Result> AddAsync(int postId, int userId, string content)
    {
        var text = content?.Trim() ?? string.Empty;

        if (text.Length < 2)
            return Result.Fail("Yorum boş olamaz.");

        if (text.Length > MaxLength)
            return Result.Fail($"Yorum en fazla {MaxLength} karakter olabilir.");

        var postIsPublished = await context.BlogPosts
            .AnyAsync(p => p.Id == postId && p.Status == BlogPostStatus.Approved);
        if (!postIsPublished)
            return Result.Fail("Yazı bulunamadı.");

        var user = await context.Users.FindAsync(userId);
        if (user is null || !user.IsActive)
            return Result.Fail("Yorum yapabilmek için hesabınızın aktif olması gerekir.");

        context.Comments.Add(new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = text,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        });
        await context.SaveChangesAsync();

        return Result.Ok("Yorumunuz eklendi.");
    }

    public async Task<Result> DeleteAsync(int commentId, int userId)
    {
        var comment = await context.Comments.FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment is null || comment.UserId != userId)
            return Result.Fail("Yorum bulunamadı.");

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();

        return Result.Ok("Yorum silindi.");
    }
}
