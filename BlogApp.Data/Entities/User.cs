namespace BlogApp.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public bool IsActive { get; set; } = false;
    public bool IsEmailVerified { get; set; } = false;

    //Navigation properties
    public List<BlogPost> Posts { get; set; } = default!;
    public virtual List<UserRole> UserRoles { get; set; } = default!;
    public virtual List<VerificationCode> VerificationCodes { get; set; } = default!;

}
