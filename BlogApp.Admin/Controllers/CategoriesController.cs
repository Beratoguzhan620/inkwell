using BlogApp.Application.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace BlogApp.Admin.Controllers;

[Route("categories")]
public class CategoriesController(IAdminService adminService, ICategoryService categoryService) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var categories = await categoryService.GetAllCategoriesAsync();
        return View(categories.Data ?? []);
    }

    [HttpPost("")]
    public async Task<IActionResult> Create(string? name) =>
        Done(await adminService.CreateCategoryAsync(name ?? string.Empty), null);

    [HttpPost("{id:int}/rename")]
    public async Task<IActionResult> Rename(int id, string? name) =>
        Done(await adminService.RenameCategoryAsync(id, name ?? string.Empty), null);

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id) =>
        Done(await adminService.DeleteCategoryAsync(id), null);
}
