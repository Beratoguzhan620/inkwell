using BlogApp.Application.DTOs.Blog;

namespace BlogApp.MVC.Models.BlogPost;

public class PostListViewModel
{
    public PagedResult<BlogPostDto> Result { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = [];
    public string? Search { get; set; }
    public int? CategoryId { get; set; }

    public CategoryDto? SelectedCategory => Categories.FirstOrDefault(c => c.Id == CategoryId);
    public bool IsFiltered => !string.IsNullOrWhiteSpace(Search) || CategoryId is not null;
}
