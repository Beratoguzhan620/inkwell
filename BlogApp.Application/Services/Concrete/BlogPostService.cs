using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using BlogApp.Data;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Application.Services.Concrete;

public class BlogPostService(AppDbContext context) : IBlogPostService
{
    public async Task<Result> CreateBlogPostAsync(CreateBlogPostRequest request)
    {
        var user = await context.Users.FindAsync(request.AuthorId);

        if (user is null || !user.IsActive)
            return Result.Fail("Makale oluşturabilmek için aktif bir kullanıcı olmanız gerekmektedir");

        var blogPost = new BlogPost
        {
            Title = request.Title,
            Content = request.Content,
            AuthorId = request.AuthorId,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.Now,
            Status = BlogPostStatus.Pending
        };

        context.BlogPosts.Add(blogPost);
        await context.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<List<BlogPostDto>>> GetAllBlogPosts()
    {
        var blogPosts = await context.BlogPosts
                    .Include(bp => bp.Category)
                    .Include(bp => bp.Author)
                    .Where(bp => bp.Status == BlogPostStatus.Approved)
                    .Select(bp => new BlogPostDto
                    {
                        Id = bp.Id,
                        Title = bp.Title,
                        Content = bp.Content,
                        CategoryId = bp.CategoryId,
                        CategoryName = bp.Category.Name,
                        AuthorId = bp.AuthorId,
                        AuthorFullName = $"{bp.Author.FirstName} {bp.Author.LastName}",
                        CreatedAt = bp.CreatedAt
                    }).ToListAsync();

        return Result<List<BlogPostDto>>.Ok(blogPosts);
    }

    public async Task<Result<BlogPostDto>> GetBlogPostById(int postId)
    {
        var blogPost = await context.BlogPosts
             .Include(bp => bp.Category)
             .Include(bp => bp.Author)
             .FirstOrDefaultAsync(bp => bp.Id == postId && bp.Status == BlogPostStatus.Approved);

        if (blogPost == null)
            return Result<BlogPostDto>.Fail("Blog post not found");

        var blogPostDto = new BlogPostDto
        {
            Id = blogPost.Id,
            Title = blogPost.Title,
            Content = blogPost.Content,
            CategoryId = blogPost.CategoryId,
            CategoryName = blogPost.Category.Name,
            AuthorId = blogPost.AuthorId,
            AuthorFullName = $"{blogPost.Author.FirstName} {blogPost.Author.LastName}",
            CreatedAt = blogPost.CreatedAt
        };

        return Result<BlogPostDto>.Ok(blogPostDto);

    }
}
