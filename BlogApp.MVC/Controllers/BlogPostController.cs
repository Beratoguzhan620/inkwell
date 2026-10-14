using BlogApp.Application.DTOs.Blog;
using BlogApp.Application.Services.Abstract;
using BlogApp.MVC.Models.BlogPost;
using BlogApp.Data.Constants;
using BlogApp.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace BlogApp.MVC.Controllers;

public class BlogPostController(
    IBlogPostService blogPostService,
    ICategoryService categoryService,
    ICommentService commentService,
    CoverImageStorage coverStorage) : Controller
{
    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("/posts/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var result = await blogPostService.GetBySlugAsync(slug, CurrentUserId);

        if (!result.Success || result.Data is null)
            return NotFound();

        return View(result.Data);
    }

    /// <summary>Old id-based links keep working.</summary>
    [HttpGet("/BlogPost/Content/{id:int}")]
    public async Task<IActionResult> Content(int id)
    {
        var slug = await blogPostService.GetSlugAsync(id);
        return slug is null ? NotFound() : RedirectToActionPermanent(nameof(Detail), new { slug });
    }

    [Authorize(Roles = RoleConstants.CanWrite)]
    [HttpGet("/my-posts")]
    public async Task<IActionResult> Mine()
    {
        return View(await blogPostService.GetAuthorPostsAsync(CurrentUserId!.Value));
    }

    [Authorize(Roles = RoleConstants.CanWrite)]
    [HttpGet("/write")]
    public async Task<IActionResult> Create()
    {
        await FillCategorySelectAsync();
        return View("Editor", new PostEditorViewModel());
    }

    [Authorize(Roles = RoleConstants.CanWrite)]
    [HttpPost("/write")]
    public async Task<IActionResult> Create(PostEditorViewModel model, string? intent)
    {
        var coverError = CoverImageStorage.Validate(model.Cover);
        if (coverError is not null)
            ModelState.AddModelError(nameof(model.Cover), coverError);

        if (!ModelState.IsValid)
            return await EditorAgainAsync(model);

        var result = await blogPostService.CreateAsync(new SaveBlogPostRequest
        {
            AuthorId = CurrentUserId!.Value,
            CategoryId = model.CategoryId,
            Title = model.Title,
            Content = model.Content,
            CoverImageUrl = model.Cover is { Length: > 0 } ? await coverStorage.SaveAsync(model.Cover) : null,
            Publish = intent == "publish",
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await EditorAgainAsync(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Detail), new { slug = result.Data });
    }

    [Authorize(Roles = RoleConstants.CanWrite)]
    [HttpGet("/posts/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await blogPostService.GetForEditAsync(id, CurrentUserId!.Value);
        if (!result.Success || result.Data is null)
            return NotFound();

        var post = result.Data;
        await FillCategorySelectAsync();

        return View("Editor", new PostEditorViewModel
        {
            Id = post.Id,
            CategoryId = post.CategoryId,
            Title = post.Title,
            Content = post.Content,
            CurrentCoverUrl = post.CoverImageUrl,
            IsPublished = post.IsPublished,
            Slug = post.Slug,
        });
    }

    [Authorize(Roles = RoleConstants.CanWrite)]
    [HttpPost("/posts/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, PostEditorViewModel model, string? intent)
    {
        model.Id = id;

        var coverError = CoverImageStorage.Validate(model.Cover);
        if (coverError is not null)
            ModelState.AddModelError(nameof(model.Cover), coverError);

        if (!ModelState.IsValid)
            return await EditorAgainAsync(model);

        var result = await blogPostService.UpdateAsync(id, new SaveBlogPostRequest
        {
            AuthorId = CurrentUserId!.Value,
            CategoryId = model.CategoryId,
            Title = model.Title,
            Content = model.Content,
            CoverImageUrl = model.Cover is { Length: > 0 } ? await coverStorage.SaveAsync(model.Cover) : null,
            RemoveCover = model.RemoveCover,
            Publish = intent == "publish",
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await EditorAgainAsync(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Detail), new { slug = result.Data });
    }

    [Authorize(Roles = RoleConstants.CanWrite)]
    [HttpPost("/posts/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await blogPostService.DeleteAsync(id, CurrentUserId!.Value);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Mine));
    }

    [Authorize]
    [HttpPost("/posts/{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, string? content)
    {
        var result = await commentService.AddAsync(id, CurrentUserId!.Value, content ?? string.Empty);
        return await BackToCommentsAsync(id, result.Success ? null : result.Message);
    }

    [Authorize]
    [HttpPost("/posts/{id:int}/comments/{commentId:int}/delete")]
    public async Task<IActionResult> DeleteComment(int id, int commentId)
    {
        var result = await commentService.DeleteAsync(commentId, CurrentUserId!.Value);
        return await BackToCommentsAsync(id, result.Success ? null : result.Message);
    }

    private async Task<IActionResult> BackToCommentsAsync(int postId, string? error)
    {
        if (error is not null)
            TempData["CommentError"] = error;

        var slug = await blogPostService.GetSlugAsync(postId);
        if (slug is null)
            return NotFound();

        return Redirect(Url.Action(nameof(Detail), new { slug }) + "#comments");
    }

    private async Task<IActionResult> EditorAgainAsync(PostEditorViewModel model)
    {
        if (model.Id is int id)
        {
            var current = await blogPostService.GetForEditAsync(id, CurrentUserId!.Value);
            if (!current.Success || current.Data is null)
                return NotFound();

            model.CurrentCoverUrl = current.Data.CoverImageUrl;
            model.IsPublished = current.Data.IsPublished;
            model.Slug = current.Data.Slug;
        }

        await FillCategorySelectAsync();
        return View("Editor", model);
    }

    private async Task FillCategorySelectAsync()
    {
        var categories = await categoryService.GetAllCategoriesAsync();
        ViewBag.CategorySelectList = new SelectList(categories.Data ?? [], "Id", "Name");
    }
}
