namespace BlogApp.Application.DTOs.Blog;

/// <summary>Create or update a post.</summary>
public class SaveBlogPostRequest
{
    public int AuthorId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>New cover URL, or null to keep the current one.</summary>
    public string? CoverImageUrl { get; set; }

    public bool RemoveCover { get; set; }

    /// <summary>True publishes the post; false keeps it as a draft.</summary>
    public bool Publish { get; set; }
}

public class BlogPostEditDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public bool IsPublished { get; set; }
    public string Slug { get; set; } = string.Empty;
}
