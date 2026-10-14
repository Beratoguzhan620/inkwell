using System.ComponentModel.DataAnnotations;

namespace BlogApp.Application.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Display(Name = "E-posta")]
    [Required, MaxLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;

    [Display(Name = "Yeni şifre")]
    [Required, MinLength(6), MaxLength(100), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Yeni şifre (tekrar)")]
    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Outcome of a mail-sending step. <see cref="Link"/> is filled only so Development can show it
/// when no SMTP server is configured; it is never displayed elsewhere.
/// </summary>
public class EmailDispatch
{
    public bool Sent { get; set; }
    public string? Link { get; set; }
    public string? Code { get; set; }
}
