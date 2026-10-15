using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using BlogApp.Core.Security;
using BlogApp.Data;
using BlogApp.Data.Constants;
using BlogApp.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace BlogApp.Application.Tests;

/// <summary>
/// A fresh in-memory SQLite database per test. SQLite (unlike the EF in-memory provider) runs real SQL,
/// so ExecuteUpdate, unique indexes and foreign keys behave like they do on SQL Server.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public const string Password = "Demo123!";

    private readonly SqliteConnection connection;

    public TestDatabase()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        Context = CreateContext();
        Context.Database.EnsureCreated();

        Context.Roles.AddRange(RoleConstants.All.Select(r => new Role { Id = r.Key, Name = r.Value }));
        Context.Categories.AddRange(new Category { Name = "Yazılım" }, new Category { Name = "Kariyer" });
        Context.SaveChanges();
    }

    public AppDbContext Context { get; }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));

    public FakeMailService Mail { get; } = new();

    public int SoftwareCategoryId => Context.Categories.Single(c => c.Name == "Yazılım").Id;

    public int CareerCategoryId => Context.Categories.Single(c => c.Name == "Kariyer").Id;

    /// <summary>A second context on the same database, to read what was really saved.</summary>
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    public User AddUser(string email, bool verified = true, bool active = true, int roleId = RoleConstants.AuthorId)
    {
        var hash = HashingHelper.CreatePasswordHash(Password);
        var user = new User
        {
            FirstName = "Deniz",
            LastName = "Yılmaz",
            Email = email,
            PasswordHash = hash.Hash,
            PasswordSalt = hash.Salt,
            IsEmailVerified = verified,
            IsActive = active,
            UserRoles = verified ? [new UserRole { RoleId = roleId }] : [],
        };

        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    public void Dispose()
    {
        Context.Dispose();
        connection.Dispose();
    }
}

public sealed class FakeMailService : IMailService
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];

    public bool Fail { get; set; }

    public Task<Result> SendEmailAsync(string to, string subject, string body)
    {
        if (Fail)
            return Task.FromResult(Result.Fail("SMTP is not configured."));

        Sent.Add((to, subject, body));
        return Task.FromResult(Result.Ok());
    }
}
