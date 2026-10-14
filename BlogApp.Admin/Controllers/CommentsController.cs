using BlogApp.Admin.Models;
using BlogApp.Application.DTOs.Admin;
using BlogApp.Application.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace BlogApp.Admin.Controllers;

[Route("comments")]
public class CommentsController(IAdminService adminService) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] AdminCommentQuery query) =>
        View(new CommentListViewModel { Query = query, Result = await adminService.GetCommentsAsync(query) });

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromForm] int[] ids, string? returnUrl) =>
        Done(await adminService.DeleteCommentsAsync(ids), returnUrl);
}
