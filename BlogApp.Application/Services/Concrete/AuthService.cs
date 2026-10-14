using System.Net;
using System.Security.Cryptography;
using BlogApp.Application.DTOs.Auth;
using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using BlogApp.Core.Security;
using BlogApp.Data;
using BlogApp.Data.Constants;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Application.Services.Concrete;

public class AuthService(AppDbContext context, IMailService mailService, TimeProvider timeProvider) : IAuthService
{
    public static readonly TimeSpan VerificationLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    private const string WrongCredentials = "E-posta adresi veya şifre hatalı.";

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim());

        if (user is null || !HashingHelper.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            return Result<LoginResponse>.Fail(WrongCredentials);

        var response = ToLoginResponse(user);

        if (!user.IsEmailVerified)
            return Result<LoginResponse>.Fail("E-posta adresiniz henüz doğrulanmadı. Size gönderilen kodu girin.", response);

        if (!user.IsActive)
            return Result<LoginResponse>.Fail("Hesabınız devre dışı bırakılmış. Yöneticiyle iletişime geçin.");

        return Result<LoginResponse>.Ok(response);
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, string verifyLinkBase)
    {
        var email = request.Email.Trim();

        if (await context.Users.AnyAsync(u => u.Email == email))
            return Result<RegisterResponse>.Fail("Bu e-posta adresiyle kayıtlı bir hesap zaten var.");

        var hash = HashingHelper.CreatePasswordHash(request.Password);
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = hash.Hash,
            PasswordSalt = hash.Salt,
            IsActive = false,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var dispatch = await IssueVerificationAsync(user, verifyLinkBase);

        return Result<RegisterResponse>.Ok(new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            VerificationEmailSent = dispatch.Sent,
            VerificationCode = dispatch.Code ?? string.Empty,
        });
    }

    public async Task<Result> VerifyUserAsync(VerificationRequest request)
    {
        var code = await context.VerificationCodes
            .Include(v => v.User)
                .ThenInclude(u => u.UserRoles)
            .Where(v => v.UserId == request.UserId
                && v.VerificationType == VerificationType.EmailVerification
                && !v.IsUsed)
            .OrderByDescending(v => v.ExpirationDate)
            .FirstOrDefaultAsync(v => v.Code == request.Code.Trim());

        if (code is null)
            return Result.Fail("Doğrulama kodu geçersiz.");

        if (code.ExpirationDate < UtcNow)
            return Result.Fail("Doğrulama kodunun süresi dolmuş. Yeni bir kod isteyin.");

        code.IsUsed = true;
        code.User.IsEmailVerified = true;
        code.User.IsActive = true;

        if (code.User.UserRoles.Count == 0)
            context.UserRoles.Add(new UserRole { UserId = code.UserId, RoleId = RoleConstants.AuthorId });

        await context.SaveChangesAsync();
        return Result.Ok("E-posta adresiniz doğrulandı.");
    }

    public async Task<Result<EmailDispatch>> ResendVerificationAsync(int userId, string verifyLinkBase)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null || user.IsEmailVerified)
            return Result<EmailDispatch>.Ok(new EmailDispatch(), "Hesap zaten doğrulanmış olabilir; giriş yapmayı deneyin.");

        var lastIssued = await context.VerificationCodes
            .Where(v => v.UserId == userId && v.VerificationType == VerificationType.EmailVerification)
            .MaxAsync(v => (DateTime?)v.ExpirationDate);

        if (lastIssued is not null && lastIssued.Value - VerificationLifetime + ResendCooldown > UtcNow)
            return Result<EmailDispatch>.Fail("Yeni kod için bir dakika bekleyin.");

        var dispatch = await IssueVerificationAsync(user, verifyLinkBase);
        return Result<EmailDispatch>.Ok(dispatch, "Yeni doğrulama kodu gönderildi.");
    }

    public async Task<Result<EmailDispatch>> RequestPasswordResetAsync(string email, string resetLinkBase)
    {
        const string message = "Bu adrese kayıtlı bir hesap varsa şifre sıfırlama bağlantısı gönderildi.";

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email.Trim() && u.IsEmailVerified);
        if (user is null)
            return Result<EmailDispatch>.Ok(new EmailDispatch(), message);

        // Older unused links stop working once a new one is issued.
        await context.VerificationCodes
            .Where(v => v.UserId == user.Id && v.VerificationType == VerificationType.PasswordReset && !v.IsUsed)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsUsed, true));

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        context.VerificationCodes.Add(new VerificationCode
        {
            UserId = user.Id,
            Code = token,
            VerificationType = VerificationType.PasswordReset,
            ExpirationDate = UtcNow.Add(ResetLifetime),
        });
        await context.SaveChangesAsync();

        var link = $"{resetLinkBase}?token={token}";
        var mail = await mailService.SendEmailAsync(user.Email, "Inkwell şifre sıfırlama", $"""
            <h1>Şifrenizi sıfırlayın</h1>
            <p>Merhaba {WebUtility.HtmlEncode(user.FirstName)}, Inkwell hesabınız için şifre sıfırlama isteği aldık.</p>
            <p><a href="{link}">Yeni şifre belirleyin</a></p>
            <p>Bağlantı 1 saat geçerlidir. Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>
            """);

        return Result<EmailDispatch>.Ok(new EmailDispatch { Sent = mail.Success, Link = link }, message);
    }

    public async Task<Result> CheckResetTokenAsync(string token) =>
        await FindResetCodeAsync(token) is null
            ? Result.Fail("Bağlantı geçersiz veya süresi dolmuş. Yeni bir bağlantı isteyin.")
            : Result.Ok();

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var code = await FindResetCodeAsync(request.Token);
        if (code is null)
            return Result.Fail("Bağlantı geçersiz veya süresi dolmuş. Yeni bir bağlantı isteyin.");

        var hash = HashingHelper.CreatePasswordHash(request.Password);
        code.User.PasswordHash = hash.Hash;
        code.User.PasswordSalt = hash.Salt;
        code.IsUsed = true;

        await context.SaveChangesAsync();
        return Result.Ok("Şifreniz güncellendi. Yeni şifrenizle giriş yapabilirsiniz.");
    }

    private Task<VerificationCode?> FindResetCodeAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult<VerificationCode?>(null);

        var now = UtcNow;
        return context.VerificationCodes
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Code == token
                && v.VerificationType == VerificationType.PasswordReset
                && !v.IsUsed
                && v.ExpirationDate > now);
    }

    private async Task<EmailDispatch> IssueVerificationAsync(User user, string verifyLinkBase)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        context.VerificationCodes.Add(new VerificationCode
        {
            UserId = user.Id,
            Code = code,
            VerificationType = VerificationType.EmailVerification,
            ExpirationDate = UtcNow.Add(VerificationLifetime),
        });
        await context.SaveChangesAsync();

        var link = $"{verifyLinkBase}?userId={user.Id}&code={code}";
        var mail = await mailService.SendEmailAsync(user.Email, "Inkwell hesap doğrulama", $"""
            <h1>Inkwell'e hoş geldiniz</h1>
            <p>Merhaba {WebUtility.HtmlEncode(user.FirstName)}, hesabınızı doğrulamak için bağlantıya tıklayın:</p>
            <p><a href="{link}">Hesabımı doğrula</a></p>
            <p>Ya da bu kodu girin: <b style="font-size:20px;letter-spacing:4px">{code}</b></p>
            <p>Kod 24 saat geçerlidir.</p>
            """);

        return new EmailDispatch { Sent = mail.Success, Link = link, Code = code };
    }

    private static LoginResponse ToLoginResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        IsActive = user.IsActive,
        IsEmailVerified = user.IsEmailVerified,
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
    };
}
