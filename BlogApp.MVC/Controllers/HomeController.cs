using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Abstract;
using BlogApp.MVC.Models;
using BlogApp.MVC.Models.BlogPost;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BlogApp.MVC.Controllers;

public class HomeController(IBlogPostService blogPostService, ICategoryService categoryService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] string? q = null, [FromQuery] int? category = null, [FromQuery] int page = 1)
    {
        var search = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var result = await blogPostService.GetPublishedAsync(new PostListQuery
        {
            Search = search,
            CategoryId = category,
            Page = page,
        });
        var categories = await categoryService.GetAllCategoriesAsync();

        return View(new PostListViewModel
        {
            Result = result,
            Categories = categories.Data ?? [],
            Search = search,
            CategoryId = category,
        });
    }

    [Route("/not-found")]
    public IActionResult PageNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }

    [HttpGet("/access-denied")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("AccessDenied");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
