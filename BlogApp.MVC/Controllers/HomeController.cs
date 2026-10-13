using BlogApp.Application.Services.Abstract;
using BlogApp.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BlogApp.MVC.Controllers;

public class HomeController(IBlogPostService blogPostService) : Controller
{
    public async Task<IActionResult> IndexAsync()
    {
        var result = await blogPostService.GetAllBlogPosts();

        return View(result.Data ?? []);
    }

    [HttpGet("/not-found")]
    public IActionResult PageNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
