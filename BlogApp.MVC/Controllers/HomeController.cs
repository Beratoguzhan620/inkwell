using BlogApp.Application.Services.Abstract;
using BlogApp.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BlogApp.MVC.Controllers;

public class HomeController(IBlogPostService blogPostService) : Controller
{
    public async Task<IActionResult> IndexAsync()
    {
        var result  = await blogPostService.GetAllBlogPosts();
        
        var posts = result.Data;

        return View(posts);
    }
}
