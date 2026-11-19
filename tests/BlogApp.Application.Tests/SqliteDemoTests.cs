using BlogApp.Data;
using BlogApp.Data.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApp.Application.Tests;

/// <summary>The public demo's set-up: the site on a SQLite file, created and filled with the demo content.</summary>
public sealed class SqliteDemoTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"inkwell-demo-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(file);
    }

    [Fact]
    public async Task The_demo_database_is_created_and_seeded_once()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={file}",
        }).Build();
        await using var services = new ServiceCollection().AddDataServices(configuration).BuildServiceProvider();

        for (var start = 0; start < 2; start++)
        {
            // Each start-up finds what the previous one made and adds nothing.
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            await DbSeeder.SeedAsync(context);
            await DemoContentSeeder.SeedAsync(context);
        }

        await using var check = services.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.BlogPosts.CountAsync() >= 20);
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == "admin@example.com"));
    }

    [Fact]
    public async Task The_native_sqlite_is_the_patched_build()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select sqlite_version()";

        var version = Version.Parse((string)(await command.ExecuteScalarAsync())!);

        // CVE-2025-6965 is fixed in SQLite 3.50.2.
        Assert.True(version >= new Version(3, 50, 2), $"SQLite {version} is loaded.");
    }
}
