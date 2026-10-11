namespace BlogApp.Data.Entities;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int AuthorId { get; set; }
    public BlogPostStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }


    //Navigation properties
    public virtual Category Category { get; set; } = default!;
    public virtual User Author { get; set; } = default!;

}

public enum BlogPostStatus
{
    Pending,
    Approved,
    Rejected
}