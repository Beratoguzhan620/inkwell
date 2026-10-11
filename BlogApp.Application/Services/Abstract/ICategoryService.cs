using BlogApp.Application.DTOs.Blog;
using BlogApp.Core.Results;


namespace BlogApp.Application.Services.Abstract;

public interface ICategoryService
{
    Task<Result<List<CategoryDto>>> GetAllCategoriesAsync();
}
