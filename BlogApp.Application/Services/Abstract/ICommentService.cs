using BlogApp.Core.Results;

namespace BlogApp.Application.Services.Abstract;

public interface ICommentService
{
    Task<Result> AddAsync(int postId, int userId, string content);

    Task<Result> DeleteAsync(int commentId, int userId);
}
