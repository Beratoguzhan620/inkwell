using BlogApp.Application.DTOs.Blog;
using BlogApp.Core.Results;

namespace BlogApp.Application.Services.Abstract;

public interface ICategoryService
{
    /// <summary>All categories with the number of published posts in each.</summary>
    Task<Result<List<CategoryDto>>> GetAllCategoriesAsync();
}
