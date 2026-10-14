using BlogApp.Core.Security;
using BlogApp.Data.Constants;
using BlogApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Data.Seeders;

public class DbSeeder
{

    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedCategoriesAsync(context);
    }

    /// <summary>
    /// Makes sure the three roles exist with their current names. Older databases used
    /// "Moderator" and "VerifiedUser" for ids 2 and 3; those rows are renamed in place.
    /// </summary>
    public static async Task SeedRolesAsync(AppDbContext context)
    {
        foreach (var (id, name) in RoleConstants.All)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id);

            if (role is null)
                context.Roles.Add(new Role { Id = id, Name = name });
            else if (role.Name != name)
                role.Name = name;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Creates the first administrator from configuration (Admin:Email, Admin:Password) if it does not exist.</summary>
    public static async Task SeedAdminAsync(AppDbContext context, string? email, string? password, string firstName = "Site", string lastName = "Yöneticisi")
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (await context.Users.AnyAsync(u => u.Email == email))
            return;

        var hash = HashingHelper.CreatePasswordHash(password);
        context.Users.Add(new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = hash.Hash,
            PasswordSalt = hash.Salt,
            IsActive = true,
            IsEmailVerified = true,
            UserRoles = [new UserRole { RoleId = RoleConstants.AdminId }],
        });

        await context.SaveChangesAsync();
    }

    public static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new() { Name = "Teknoloji" },
            new() { Name = "Yazılım" },
            new() { Name = "Bilim" },
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }
}
