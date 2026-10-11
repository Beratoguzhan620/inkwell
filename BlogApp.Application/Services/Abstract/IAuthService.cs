using BlogApp.Application.DTOs.Auth;
using BlogApp.Core.Results;
using BlogApp.Data.Entities;

namespace BlogApp.Application.Services.Abstract;

public interface IAuthService
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result> VerifyUserAsync(VerificationRequest request);
}
