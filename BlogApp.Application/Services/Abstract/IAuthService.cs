using BlogApp.Application.DTOs.Auth;
using BlogApp.Core.Results;

namespace BlogApp.Application.Services.Abstract;

public interface IAuthService
{
    /// <param name="verifyLinkBase">Absolute URL of the verification link endpoint.</param>
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, string verifyLinkBase);

    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);

    Task<Result> VerifyUserAsync(VerificationRequest request);

    Task<Result<EmailDispatch>> ResendVerificationAsync(int userId, string verifyLinkBase);

    /// <summary>Always reports success so the form does not reveal which e-mails are registered.</summary>
    Task<Result<EmailDispatch>> RequestPasswordResetAsync(string email, string resetLinkBase);

    Task<Result> CheckResetTokenAsync(string token);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
}
