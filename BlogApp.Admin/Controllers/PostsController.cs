using BlogApp.Admin.Models;
using BlogApp.Application.DTOs.Admin;
using BlogApp.Application.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace BlogApp.Admin.Controllers;

[Route("posts")]
public class PostsController(IAdminService adminService, ICategoryService categoryService) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] AdminPostQuery query)
    {
        var categories = await categoryService.GetAllCategoriesAsync();
        return View(new PostListViewModel
        {
            Query = query,
            Result = await adminService.GetPostsAsync(query),
            Categories = categories.Data ?? [],
        });
    }

    [HttpPost("{id:int}/hide")]
    public async Task<IActionResult> Hide(int id, string? returnUrl) =>
        Done(await adminService.SetPostHiddenAsync(id, hidden: true), returnUrl);

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id, string? returnUrl) =>
        Done(await adminService.SetPostHiddenAsync(id, hidden: false), returnUrl);

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, string? returnUrl) =>
        Done(await adminService.DeletePostAsync(id), returnUrl);
}
