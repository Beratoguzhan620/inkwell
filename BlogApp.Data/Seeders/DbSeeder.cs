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

    public static async Task SeedRolesAsync(AppDbContext context)
    {
        var roleIds = new[] { RoleConstants.AdminId, RoleConstants.ModeratorId, RoleConstants.VerifiedUserId };

        if (await context.Roles.AnyAsync(r => roleIds.Contains(r.Id)))
            return;

        var roles = new List<Role>
        {
            new() { Id = RoleConstants.AdminId, Name = RoleConstants.Admin },
            new() { Id = RoleConstants.ModeratorId, Name = RoleConstants.Moderator },
            new() { Id = RoleConstants.VerifiedUserId, Name = RoleConstants.VerifiedUser },
        };

        await context.Roles.AddRangeAsync(roles);
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
