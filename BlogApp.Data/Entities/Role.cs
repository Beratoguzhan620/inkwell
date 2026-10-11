namespace BlogApp.Data.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    //Navigation properties
    public virtual List<UserRole> UserRoles { get; set; } = default!;
}
