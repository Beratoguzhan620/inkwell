using System;
using System.Collections.Generic;
using System.Text;

namespace BlogApp.Application.DTOs.Blog;

public class CreateBlogPostRequest
{
    public int AuthorId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
