using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Abstract;
using BlogApp.MVC.Models.BlogPost;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace BlogApp.MVC.Controllers;

public class BlogPostController(IBlogPostService blogPostService, ICategoryService categoryService) : Controller
{
    public async Task<IActionResult> Content(int id)
    {
        var result = await blogPostService.GetBlogPostById(id);

        if (!result.Success || result.Data is null)
            return RedirectToAction("Index", "Home");

        var posts = result.Data;

        return View(posts);
    }

    [Authorize]
    public async Task<IActionResult> Create()
    {
        await FillCategorySelectAsync();



        return View();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateAsync(BlogPostCreateViewModel model)
    {
        //TODO: blogPostService.CreateBlogPost() metodu tetiklensin, authorId user'in claimlerinden gelsin 

        if (!ModelState.IsValid)
            return View(model);


        var request = new CreateBlogPostRequest
        {
            CategoryId = model.CategoryId,
            Title = model.Title,
            Content = model.Content,
            AuthorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")
        };

        var result = await blogPostService.CreateBlogPostAsync(request);

        if(!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            await FillCategorySelectAsync(); //Hata durumunda select alanının tekrar doldurulması gerekiyor
        }

        return RedirectToAction(nameof(Create));
    }

    private async Task FillCategorySelectAsync()
    {
        //TODO: db'den gelen kategoriler ekrandaki drowdownu doldursun
        var categoriesResult = await categoryService.GetAllCategoriesAsync();

        if (categoriesResult.Success)
        {
            var categorySelectList = new SelectList(categoriesResult.Data, "Id", "Name");
            ViewBag.CategorySelectList = categorySelectList;
        }
    }
}
