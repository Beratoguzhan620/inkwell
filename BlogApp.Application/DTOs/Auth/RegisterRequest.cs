using System.ComponentModel.DataAnnotations;

namespace BlogApp.Application.DTOs.Auth;

public class RegisterRequest
{
    [Display(Name = "Ad")]
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required, MaxLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required, MinLength(6), MaxLength(100), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
