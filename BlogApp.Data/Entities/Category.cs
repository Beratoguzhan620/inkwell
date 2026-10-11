namespace BlogApp.Data.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    //Navigation properties
    public virtual List<BlogPost> Posts { get; set; } = default!;
}
