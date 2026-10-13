using BlogApp.Application.DTOs.Auth;
using BlogApp.Application.Services.Abstract;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogApp.MVC.Controllers;

public class AuthController(IAuthService authService, IWebHostEnvironment environment) : Controller
{
    [HttpGet("Register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);


        var result = await authService.RegisterAsync(model);

        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(model);
        }

        var registration = result.Data!;
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
            ViewBag.ErrorMessage = result.Message;
            return View(model);
        }

        await CookieAuthSignIn(result, model.RememberMe);


        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

       
        return RedirectToAction("Index", "Home");
    }


    [HttpGet("Verify")]
    public IActionResult Verify(int userId)
    {
        return View(new VerificationRequest { UserId = userId });
    }

    [HttpPost("Verify")]
    public async Task<IActionResult> Verify(VerificationRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);


        var request = new VerificationRequest
        {
            UserId = model.UserId,
            Code = model.Code
        };

        var result = await authService.VerifyUserAsync(request);

        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(model);
        }

        return RedirectToAction(nameof(VerificationSuccessful));
    }

    [HttpGet("verify/link")]
    public async Task<IActionResult> VerifyWithLinkAsync(int userId, string code)
    {
        var request = new VerificationRequest
        {
            UserId = userId,
            Code = code
        };

        var result = await authService.VerifyUserAsync(request);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Verify), new { userId });
        }

        return RedirectToAction(nameof(VerificationSuccessful));

    }

    [HttpGet("Verify/Done")]
    public IActionResult VerificationSuccessful()
    {
        return View();
    }

    private async Task CookieAuthSignIn(Core.Results.Result<LoginResponse> result, bool rememberMe)
    {
        var claims = new List<Claim>();

        claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Data!.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, result.Data.FirstName));
        claims.Add(new Claim(ClaimTypes.Surname, result.Data.LastName));
        claims.Add(new Claim(ClaimTypes.Email, result.Data.Email));
        claims.Add(new Claim("FullName", $"{result.Data.FirstName} {result.Data.LastName}"));
        claims.Add(new Claim("IsActive", result.Data.IsActive.ToString()));

        foreach (var role in result.Data.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTime.UtcNow.AddHours(3),
            AllowRefresh = true,
        };

        await HttpContext.SignInAsync(principal, authProperties);
    }


    [HttpGet("Logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }
}
