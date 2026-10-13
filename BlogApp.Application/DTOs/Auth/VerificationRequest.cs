using System.ComponentModel.DataAnnotations;

namespace BlogApp.Application.DTOs.Auth;

public class VerificationRequest
{
    public int UserId { get; set; }

    [Display(Name = "Doğrulama kodu")]
    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}
