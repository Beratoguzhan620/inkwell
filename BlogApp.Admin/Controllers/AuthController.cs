using BlogApp.Admin.Models;
using BlogApp.Application.DTOs.Auth;
using BlogApp.Application.Services.Abstract;
using BlogApp.Data.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogApp.Admin.Controllers;

[AllowAnonymous]
public class AuthController(IAuthService authService) : Controller
{
    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.IsInRole(RoleConstants.Admin))
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await authService.LoginAsync(new LoginRequest { Email = model.Email, Password = model.Password });

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Data is null ? result.Message : "E-posta adresi doğrulanmamış hesaplar giriş yapamaz.");
            return View(model);
        }

        if (!result.Data.Roles.Contains(RoleConstants.Admin))
        {
            ModelState.AddModelError(string.Empty, "Bu panel yalnızca yönetici hesapları içindir.");
            return View(model);
        }

        var user = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("FullName", $"{user.FirstName} {user.LastName}"),
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
    }

    [HttpPost("/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
