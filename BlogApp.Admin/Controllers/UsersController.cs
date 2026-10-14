using BlogApp.Admin.Models;
using BlogApp.Application.DTOs.Admin;
using BlogApp.Application.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace BlogApp.Admin.Controllers;

[Route("users")]
public class UsersController(IAdminService adminService) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] AdminUserQuery query) =>
        View(new UserListViewModel { Query = query, Result = await adminService.GetUsersAsync(query) });

    [HttpPost("{id:int}/role")]
    public async Task<IActionResult> SetRole(int id, int roleId, string? returnUrl) =>
        Done(await adminService.SetUserRoleAsync(id, roleId, CurrentUserId), returnUrl);

    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, string? returnUrl) =>
        Done(await adminService.SetUserActiveAsync(id, true, CurrentUserId), returnUrl);

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, string? returnUrl) =>
        Done(await adminService.SetUserActiveAsync(id, false, CurrentUserId), returnUrl);
}
