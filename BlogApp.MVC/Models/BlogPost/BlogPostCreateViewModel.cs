namespace BlogApp.MVC.Models.BlogPost;

public class BlogPostCreateViewModel
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
