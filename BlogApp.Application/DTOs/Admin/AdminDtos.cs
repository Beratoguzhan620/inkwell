using BlogApp.Data.Entities;

namespace BlogApp.Application.DTOs.Admin;

public class AdminDashboardDto
{
    public int PublishedPosts { get; set; }
    public int DraftPosts { get; set; }
    public int HiddenPosts { get; set; }
    public int Users { get; set; }
    public int Authors { get; set; }
    public int CommentsLast7Days { get; set; }
    public List<AdminPostRowDto> RecentPosts { get; set; } = [];
    public List<AdminCommentRowDto> RecentComments { get; set; } = [];
}

public class AdminPostRowDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public BlogPostStatus Status { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class AdminUserRowDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public int PostCount { get; set; }
}

public class AdminCommentRowDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public string PostSlug { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public abstract class AdminQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public enum PostStatusFilter
{
    All,
    Published,
    Draft,
    Hidden,
}

public class AdminPostQuery : AdminQuery
{
    public PostStatusFilter Status { get; set; }
    public int? CategoryId { get; set; }
}

public class AdminUserQuery : AdminQuery
{
    public string? Role { get; set; }
}

public class AdminCommentQuery : AdminQuery
{
}
