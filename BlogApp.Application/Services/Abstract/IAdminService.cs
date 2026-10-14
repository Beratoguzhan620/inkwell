using BlogApp.Application.DTOs.Admin;
using BlogApp.Application.DTOs.Blog;
using BlogApp.Core.Results;

namespace BlogApp.Application.Services.Abstract;

/// <summary>Read models and moderation actions for the admin panel.</summary>
public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync();

    Task<PagedResult<AdminPostRowDto>> GetPostsAsync(AdminPostQuery query);

    /// <summary>Hides a published post from readers, or restores a hidden one.</summary>
    Task<Result> SetPostHiddenAsync(int postId, bool hidden);

    Task<Result> DeletePostAsync(int postId);

    Task<PagedResult<AdminUserRowDto>> GetUsersAsync(AdminUserQuery query);

    Task<Result> SetUserRoleAsync(int userId, int roleId, int actingUserId);

    Task<Result> SetUserActiveAsync(int userId, bool active, int actingUserId);

    Task<PagedResult<AdminCommentRowDto>> GetCommentsAsync(AdminCommentQuery query);

    Task<Result> DeleteCommentsAsync(IReadOnlyCollection<int> commentIds);

    Task<Result> CreateCategoryAsync(string name);

    Task<Result> RenameCategoryAsync(int categoryId, string name);

    Task<Result> DeleteCategoryAsync(int categoryId);
}
