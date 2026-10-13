using BlogApp.Data.Entities;

namespace BlogApp.Application.DTOs.Blog;

/// <summary>A post as it appears in lists.</summary>
public class BlogPostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public string AuthorFullName { get; set; } = string.Empty;
    public BlogPostStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ReadingMinutes { get; set; }
    public int CommentCount { get; set; }

    public DateTime DisplayDate => PublishedAt ?? UpdatedAt ?? CreatedAt;
}

/// <summary>A post on its own page.</summary>
public class BlogPostDetailDto : BlogPostDto
{
    public string ContentHtml { get; set; } = string.Empty;
    public int AuthorPublishedCount { get; set; }
    public bool IsOwnedByViewer { get; set; }
    public List<CommentDto> Comments { get; set; } = [];
}

public class CommentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PostCount { get; set; }
}

public class PostListQuery
{
    public const int DefaultPageSize = 8;

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}

public class PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
