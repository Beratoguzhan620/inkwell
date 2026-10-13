using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using BlogApp.Data;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Application.Services.Concrete;

public class CategoryService(AppDbContext context) : ICategoryService
{
    public async Task<Result<List<CategoryDto>>> GetAllCategoriesAsync()
    {
        var categories = await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                PostCount = c.Posts.Count(p => p.Status == BlogPostStatus.Approved),
            })
            .ToListAsync();

        return Result<List<CategoryDto>>.Ok(categories);
    }
}
