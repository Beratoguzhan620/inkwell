using BlogApp.Application.DTOs.Blog;
using BlogApp.Core.Results;

namespace BlogApp.Application.Services.Abstract;

public interface IBlogPostService
{
    Task<Result<List<BlogPostDto>>> GetAllBlogPosts();
    Task<Result<BlogPostDto>> GetBlogPostById(int postId);
    Task<Result> CreateBlogPostAsync(CreateBlogPostRequest request);

}
