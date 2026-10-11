using System;
using System.Collections.Generic;
using System.Text;

namespace BlogApp.Application.DTOs.Blog;

public class BlogPostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public string AuthorFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
