namespace BlogApp.Data.Entities;

public class UserRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }

    //Navigation properties
    public virtual User User { get; set; } = default!;
    public virtual Role Role { get; set; } = default!;
}