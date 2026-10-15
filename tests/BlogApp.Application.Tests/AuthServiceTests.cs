using BlogApp.Application.DTOs.Auth;
using BlogApp.Application.Services.Concrete;
using BlogApp.Data.Constants;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Application.Tests;

public sealed class AuthServiceTests : IDisposable
{
    private const string VerifyLink = "https://inkwell.test/verify";
    private const string ResetLink = "https://inkwell.test/reset-password";

    private readonly TestDatabase db = new();
    private readonly AuthService service;

    public AuthServiceTests()
    {
        service = new AuthService(db.Context, db.Mail, db.Clock);
    }

    public void Dispose() => db.Dispose();

    private static RegisterRequest NewAccount(string email = "yeni@example.com") => new()
    {
        FirstName = "Ece",
        LastName = "Demir",
        Email = email,
        Password = TestDatabase.Password,
    };

    [Fact]
    public async Task Register_creates_an_inactive_account_and_mails_a_six_digit_code()
    {
        var result = await service.RegisterAsync(NewAccount(), VerifyLink);

        Assert.True(result.Success);
        Assert.False(result.Data!.IsActive);
        Assert.False(result.Data.IsEmailVerified);
        Assert.Matches("^[0-9]{6}$", result.Data.VerificationCode);

        var mail = Assert.Single(db.Mail.Sent);
        Assert.Equal("yeni@example.com", mail.To);
        Assert.Contains(result.Data.VerificationCode, mail.Body);
    }

    [Fact]
    public async Task Register_rejects_an_e_mail_that_is_already_used()
    {
        db.AddUser("yeni@example.com");

        var result = await service.RegisterAsync(NewAccount(" yeni@example.com "), VerifyLink);

        Assert.False(result.Success);
        Assert.Single(db.CreateContext().Users.Where(u => u.Email == "yeni@example.com"));
    }

    [Fact]
    public async Task Verifying_the_code_activates_the_account_and_makes_it_an_author()
    {
        var registered = (await service.RegisterAsync(NewAccount(), VerifyLink)).Data!;

        var result = await service.VerifyUserAsync(new VerificationRequest { UserId = registered.UserId, Code = registered.VerificationCode });

        Assert.True(result.Success);
        var user = await db.CreateContext().Users.Include(u => u.UserRoles).SingleAsync(u => u.Id == registered.UserId);
        Assert.True(user.IsEmailVerified);
        Assert.True(user.IsActive);
        Assert.Equal(RoleConstants.AuthorId, Assert.Single(user.UserRoles).RoleId);
    }

    [Fact]
    public async Task A_wrong_code_does_not_verify_the_account()
    {
        var registered = (await service.RegisterAsync(NewAccount(), VerifyLink)).Data!;
        var wrong = registered.VerificationCode == "000000" ? "111111" : "000000";

        var result = await service.VerifyUserAsync(new VerificationRequest { UserId = registered.UserId, Code = wrong });

        Assert.False(result.Success);
        Assert.False((await db.CreateContext().Users.SingleAsync(u => u.Id == registered.UserId)).IsEmailVerified);
    }

    [Fact]
    public async Task A_code_older_than_24_hours_is_rejected()
    {
        var registered = (await service.RegisterAsync(NewAccount(), VerifyLink)).Data!;
        db.Clock.Advance(AuthService.VerificationLifetime + TimeSpan.FromMinutes(1));

        var result = await service.VerifyUserAsync(new VerificationRequest { UserId = registered.UserId, Code = registered.VerificationCode });

        Assert.False(result.Success);
        Assert.Contains("süresi dolmuş", result.Message);
    }

    [Fact]
    public async Task A_new_code_can_be_requested_only_after_the_cooldown()
    {
        var registered = (await service.RegisterAsync(NewAccount(), VerifyLink)).Data!;

        var tooSoon = await service.ResendVerificationAsync(registered.UserId, VerifyLink);
        db.Clock.Advance(AuthService.ResendCooldown + TimeSpan.FromSeconds(1));
        var later = await service.ResendVerificationAsync(registered.UserId, VerifyLink);

        Assert.False(tooSoon.Success);
        Assert.True(later.Success);
        Assert.Equal(2, db.Mail.Sent.Count);
    }

    [Fact]
    public async Task Login_succeeds_with_the_right_password_and_returns_the_roles()
    {
        db.AddUser("yazar@example.com");

        var result = await service.LoginAsync(new LoginRequest { Email = "yazar@example.com", Password = TestDatabase.Password });

        Assert.True(result.Success);
        Assert.Equal(new[] { RoleConstants.Author }, result.Data!.Roles);
    }

    [Fact]
    public async Task Login_gives_the_same_answer_for_an_unknown_e_mail_and_a_wrong_password()
    {
        db.AddUser("yazar@example.com");

        var unknown = await service.LoginAsync(new LoginRequest { Email = "kimse@example.com", Password = TestDatabase.Password });
        var wrongPassword = await service.LoginAsync(new LoginRequest { Email = "yazar@example.com", Password = "Yanlis123!" });

        Assert.False(unknown.Success);
        Assert.False(wrongPassword.Success);
        Assert.Equal(unknown.Message, wrongPassword.Message);
    }

    [Fact]
    public async Task Login_is_refused_for_an_unverified_or_disabled_account()
    {
        db.AddUser("dogrulanmamis@example.com", verified: false, active: false);
        db.AddUser("kapali@example.com", active: false);

        var unverified = await service.LoginAsync(new LoginRequest { Email = "dogrulanmamis@example.com", Password = TestDatabase.Password });
        var disabled = await service.LoginAsync(new LoginRequest { Email = "kapali@example.com", Password = TestDatabase.Password });

        Assert.False(unverified.Success);
        Assert.NotNull(unverified.Data); // the page needs the user id to show the code form
        Assert.False(disabled.Success);
        Assert.Null(disabled.Data);
    }

    [Fact]
    public async Task Password_reset_answers_the_same_for_unknown_addresses_and_sends_nothing()
    {
        db.AddUser("yazar@example.com");

        var known = await service.RequestPasswordResetAsync("yazar@example.com", ResetLink);
        var unknown = await service.RequestPasswordResetAsync("kimse@example.com", ResetLink);

        Assert.True(known.Success);
        Assert.True(unknown.Success);
        Assert.Equal(known.Message, unknown.Message);
        Assert.Single(db.Mail.Sent);
        Assert.Null(unknown.Data!.Link);
    }

    [Fact]
    public async Task A_reset_link_changes_the_password_once()
    {
        var user = db.AddUser("yazar@example.com");
        var token = TokenFrom((await service.RequestPasswordResetAsync(user.Email, ResetLink)).Data!.Link!);

        var first = await service.ResetPasswordAsync(new ResetPasswordRequest { Token = token, Password = "YeniSifre1!", ConfirmPassword = "YeniSifre1!" });
        var second = await service.ResetPasswordAsync(new ResetPasswordRequest { Token = token, Password = "BaskaSifre2!", ConfirmPassword = "BaskaSifre2!" });

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.True((await service.LoginAsync(new LoginRequest { Email = user.Email, Password = "YeniSifre1!" })).Success);
        Assert.False((await service.LoginAsync(new LoginRequest { Email = user.Email, Password = TestDatabase.Password })).Success);
    }

    [Fact]
    public async Task Requesting_a_new_reset_link_cancels_the_previous_one()
    {
        var user = db.AddUser("yazar@example.com");
        var oldToken = TokenFrom((await service.RequestPasswordResetAsync(user.Email, ResetLink)).Data!.Link!);
        var newToken = TokenFrom((await service.RequestPasswordResetAsync(user.Email, ResetLink)).Data!.Link!);

        Assert.False((await service.CheckResetTokenAsync(oldToken)).Success);
        Assert.True((await service.CheckResetTokenAsync(newToken)).Success);
    }

    [Fact]
    public async Task A_reset_link_expires_after_an_hour()
    {
        var user = db.AddUser("yazar@example.com");
        var token = TokenFrom((await service.RequestPasswordResetAsync(user.Email, ResetLink)).Data!.Link!);

        db.Clock.Advance(AuthService.ResetLifetime + TimeSpan.FromMinutes(1));

        Assert.False((await service.CheckResetTokenAsync(token)).Success);
    }

    private static string TokenFrom(string link) => link[(link.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..];
}
