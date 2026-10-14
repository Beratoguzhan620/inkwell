using BlogApp.Application.DTOs.Auth;
using BlogApp.Application.Services.Abstract;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogApp.MVC.Controllers;

public class AuthController(IAuthService authService, IWebHostEnvironment environment) : Controller
{
    private string VerifyLinkBase => Url.Action(nameof(VerifyWithLink), "Auth", null, Request.Scheme)!;
    private string ResetLinkBase => Url.Action(nameof(ResetPassword), "Auth", null, Request.Scheme)!;

    [HttpGet("Register")]
    public IActionResult Register() => View();

    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authService.RegisterAsync(model, VerifyLinkBase);

        if (!result.Success || result.Data is null)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(model);
        }

        var registration = result.Data;
        if (!registration.VerificationEmailSent && environment.IsDevelopment())
            TempData["DevelopmentCode"] = registration.VerificationCode;
        else
            TempData["InfoMessage"] = $"{registration.Email} adresine doğrulama kodu gönderildi.";

        return RedirectToAction(nameof(Verify), new { userId = registration.UserId });
    }

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await authService.LoginAsync(model);

        if (!result.Success || result.Data is null)
        {
            // Correct password but unverified e-mail: send the user to the code screen.
            if (result.Data is { IsEmailVerified: false } pending)
            {
                TempData["InfoMessage"] = result.Message;
                return RedirectToAction(nameof(Verify), new { userId = pending.Id });
            }

            ViewBag.ErrorMessage = result.Message;
            return View(model);
        }

        await SignInAsync(result.Data, model.RememberMe);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet("Verify")]
    public IActionResult Verify(int userId) => View(new VerificationRequest { UserId = userId });

    [HttpPost("Verify")]
    public async Task<IActionResult> Verify(VerificationRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authService.VerifyUserAsync(model);

        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(model);
        }

        return RedirectToAction(nameof(VerificationSuccessful));
    }

    [HttpPost("Verify/Resend")]
    public async Task<IActionResult> ResendVerification(int userId)
    {
        var result = await authService.ResendVerificationAsync(userId, VerifyLinkBase);

        TempData[result.Success ? "InfoMessage" : "ErrorMessage"] = result.Message;
        if (result.Data is { Sent: false, Code: not null } dispatch && environment.IsDevelopment())
            TempData["DevelopmentCode"] = dispatch.Code;

        return RedirectToAction(nameof(Verify), new { userId });
    }

    [HttpGet("verify/link")]
    public async Task<IActionResult> VerifyWithLink(int userId, string code)
    {
        var result = await authService.VerifyUserAsync(new VerificationRequest { UserId = userId, Code = code ?? string.Empty });

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Verify), new { userId });
        }

        return RedirectToAction(nameof(VerificationSuccessful));
    }

    [HttpGet("Verify/Done")]
    public IActionResult VerificationSuccessful() => View();

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordRequest());

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authService.RequestPasswordResetAsync(model.Email, ResetLinkBase);

        TempData["SuccessMessage"] = result.Message;
        if (result.Data is { Sent: false, Link: not null } dispatch && environment.IsDevelopment())
            TempData["DevelopmentLink"] = dispatch.Link;

        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet("reset-password")]
    public async Task<IActionResult> ResetPassword(string? token)
    {
        var check = await authService.CheckResetTokenAsync(token ?? string.Empty);
        if (!check.Success)
        {
            TempData["ErrorMessage"] = check.Message;
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordRequest { Token = token! });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authService.ResetPasswordAsync(model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(ForgotPassword));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInAsync(LoginResponse user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Email, user.Email),
            new("FullName", $"{user.FirstName} {user.LastName}"),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(principal, new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(rememberMe ? 24 * 14 : 3),
            AllowRefresh = true,
        });
    }
}
