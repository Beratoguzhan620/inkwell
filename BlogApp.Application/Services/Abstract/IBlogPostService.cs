using BlogApp.Application.DTOs.Blog;
using BlogApp.Core.Results;

namespace BlogApp.Application.Services.Abstract;

public interface IBlogPostService
{
    Task<PagedResult<BlogPostDto>> GetPublishedAsync(PostListQuery query);

    /// <summary>A published post, or the viewer's own draft.</summary>
    Task<Result<BlogPostDetailDto>> GetBySlugAsync(string slug, int? viewerId);

    Task<string?> GetSlugAsync(int postId);

    Task<List<BlogPostDto>> GetAuthorPostsAsync(int authorId);

    Task<Result<BlogPostEditDto>> GetForEditAsync(int postId, int authorId);

    /// <returns>The slug of the saved post.</returns>
    Task<Result<string>> CreateAsync(SaveBlogPostRequest request);

    /// <returns>The slug of the saved post.</returns>
    Task<Result<string>> UpdateAsync(int postId, SaveBlogPostRequest request);

    Task<Result> DeleteAsync(int postId, int authorId);
}
