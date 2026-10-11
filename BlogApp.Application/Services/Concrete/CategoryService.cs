using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using BlogApp.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Application.Services.Concrete;

//TODO: Generic Repository pattern'i inceleyin.
public class CategoryService(AppDbContext context) : ICategoryService
{
    public async Task<Result<List<CategoryDto>>> GetAllCategoriesAsync()
    {
        var categories = await context.Categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        }).ToListAsync();

        return Result<List<CategoryDto>>.Ok(categories);
    }
}
