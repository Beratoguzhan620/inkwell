namespace BlogApp.Data.Entities;

public class VerificationCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int UserId { get; set; }
    public bool IsUsed { get; set; }
    public DateTime ExpirationDate { get; set; }
    public VerificationType VerificationType { get; set; }

    //Navigation properties
    public virtual User User { get; set; } = default!;
}


public enum VerificationType
{
    EmailVerification,
    PasswordReset,
}