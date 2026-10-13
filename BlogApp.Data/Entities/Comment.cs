namespace BlogApp.Data.Entities;

public class Comment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    //Navigation properties
    public virtual BlogPost Post { get; set; } = default!;
    public virtual User User { get; set; } = default!;
}
