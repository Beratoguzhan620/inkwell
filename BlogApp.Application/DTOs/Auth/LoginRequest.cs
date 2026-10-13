using System.ComponentModel.DataAnnotations;

namespace BlogApp.Application.DTOs.Auth;

public class LoginRequest
{
    [Display(Name = "E-posta")]
    [Required, MaxLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required, MinLength(6), MaxLength(100), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}
