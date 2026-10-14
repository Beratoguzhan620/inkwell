using BlogApp.Application.DTOs.Admin;
using BlogApp.Application.DTOs.Blog;
using System.ComponentModel.DataAnnotations;

namespace BlogApp.Admin.Models;

public record PagerViewModel(int Page, int PageSize, int TotalCount, int TotalPages)
{
    public static PagerViewModel From<T>(PagedResult<T> result) =>
        new(result.Page, result.PageSize, result.TotalCount, result.TotalPages);
}

public class PostListViewModel
{
    public PagedResult<AdminPostRowDto> Result { get; set; } = new();
    public AdminPostQuery Query { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = [];
}

public class UserListViewModel
{
    public PagedResult<AdminUserRowDto> Result { get; set; } = new();
    public AdminUserQuery Query { get; set; } = new();
}

public class CommentListViewModel
{
    public PagedResult<AdminCommentRowDto> Result { get; set; } = new();
    public AdminCommentQuery Query { get; set; } = new();
}

public class LoginViewModel
{
    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "E-posta gereklidir."), EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "Şifre gereklidir.")]
    public string Password { get; set; } = string.Empty;
}
