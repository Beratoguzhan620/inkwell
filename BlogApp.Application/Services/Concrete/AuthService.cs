using BlogApp.Application.DTOs.Auth;
using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using BlogApp.Core.Security;
using BlogApp.Data;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BlogApp.Application.Services.Concrete;

public class AuthService(AppDbContext context, IMailService mailService, IConfiguration configuration) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await context.Users
                                .Include(u => u.UserRoles)
                                .ThenInclude(ur => ur.Role)
                                .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
            return Result<LoginResponse>.Fail("Email adresi veya şifre hatalı.");


        var passwordCorrect = HashingHelper.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt);
        if (!passwordCorrect)
            return Result<LoginResponse>.Fail("Email adresi veya şifre hatalı.");


        var isEmailVerified = user.IsEmailVerified;
        if (!isEmailVerified)
            return Result<LoginResponse>.Fail("Lütfen hesabınızı mailinze gelen onay linkinden aktive ediniz.");

        var isActive = user.IsActive;
        if (!isActive)
            return Result<LoginResponse>.Fail("Hesabınız deaktive edilmiştir.");

        var response = new LoginResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };


        return Result<LoginResponse>.Ok(response);
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request)
    {
        //1. business rule (aynı email ile kayıtlı kullanıcı var mı?)
        var userExists = context.Users.Any(u => u.Email == request.Email);
        if (userExists)
            return Result<RegisterResponse>.Fail("Bu email ile kayıtlı bir kullanıcı zaten var.");

        //2. password'u hashliyeceğiz
        var result = HashingHelper.CreatePasswordHash(request.Password);

        //3. veri tabanına kaydeceğim
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = result.Hash,
            PasswordSalt = result.Salt,
            IsActive = false,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();


        var verificationCode = new VerificationCode
        {
            UserId = user.Id,
            Code = Guid.NewGuid().ToString().Substring(0, 6),
            ExpirationDate = DateTime.Now.AddHours(24),
        };


        context.VerificationCodes.Add(verificationCode);
        await context.SaveChangesAsync();


        var emailResult = await SendVerificationEmailAsync(user.Id, verificationCode.Code, user.Email);


        var response = new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            VerificationEmailSent = emailResult.Success,
            VerificationCode = verificationCode.Code,
        };

        return Result<RegisterResponse>.Ok(response);
    }

    public async Task<Result> VerifyUserAsync(VerificationRequest request)
    {
        var code = await context.VerificationCodes
            .Include(x => x.User)
            .FirstOrDefaultAsync(v =>
            v.UserId == request.UserId &&
            v.Code == request.Code &&
            !v.IsUsed);

        if (code is null)
            return Result.Fail("Onay kodu geçersiz.");

        code.User.IsEmailVerified = true;
        code.User.IsActive = true;
        code.IsUsed = true;
        await context.SaveChangesAsync();

        return Result.Ok("Hesap doğrulama başarılı.");
    }


    private async Task<Result> SendVerificationEmailAsync(int userId, string code, string email)
    {
        var domain = configuration["AppUrl"];
        var subject = "Inkwell hesap doğrulama";
        var body = $"""

            <h1>Inkwell hesap doğrulama</h1>
            <p>Hesabınızı doğrulamak için aşağıdaki linke tıklayınız:</p>
            <a href="{domain}/verify/link?userId={userId}&code={code}">Hesabımı Doğrula</a>

            <p>Bu link 24 saat geçerlidir.</p>
            <p>Kod: <b>{code}</b></p>
            """;

        var result = await mailService.SendEmailAsync(email, subject, body);
        return result;
    }
}
