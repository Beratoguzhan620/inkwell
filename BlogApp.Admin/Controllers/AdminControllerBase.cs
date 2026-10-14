using BlogApp.Core.Results;
using BlogApp.Data.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogApp.Admin.Controllers;

[Authorize(Roles = RoleConstants.Admin)]
public abstract class AdminControllerBase : Controller
{
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected IActionResult Done(Result result, string? returnUrl, string fallbackAction = "Index")
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(fallbackAction);
    }
}
