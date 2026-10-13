namespace BlogApp.Data.Entities;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>URL-friendly, unique identifier derived from the title.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Markdown source.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Short plain-text summary shown in lists.</summary>
    public string Excerpt { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }
    public int CategoryId { get; set; }
    public int AuthorId { get; set; }
    public BlogPostStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    //Navigation properties
    public virtual Category Category { get; set; } = default!;
    public virtual User Author { get; set; } = default!;
    public virtual List<Comment> Comments { get; set; } = default!;
}

public enum BlogPostStatus
{
    Pending,

    /// <summary>Visible to readers.</summary>
    Approved,

    Rejected,

    /// <summary>Only visible to its author.</summary>
    Draft,
}
