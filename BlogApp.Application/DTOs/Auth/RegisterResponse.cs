namespace BlogApp.Application.DTOs.Auth;

public class RegisterResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>True when the verification e-mail was handed to the SMTP server.</summary>
    public bool VerificationEmailSent { get; set; }

    /// <summary>
    /// The code itself, so the UI can show it in Development when no SMTP server is configured.
    /// Never displayed outside Development.
    /// </summary>
    public string VerificationCode { get; set; } = string.Empty;
}
