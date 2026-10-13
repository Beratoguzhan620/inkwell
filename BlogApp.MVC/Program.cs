using BlogApp.Data;
using BlogApp.Data.Seeders;
using BlogApp.Application;
using BlogApp.MVC;
using Microsoft.EntityFrameworkCore;
using BlogApp.Application.DTOs.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMVCServices();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Apply pending migrations and seed lookup data on start-up, so a fresh database works
// without running `dotnet ef database update` by hand.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(dbContext);

    if (app.Configuration.GetValue<bool>("Seed:DemoContent"))
        await DemoContentSeeder.SeedAsync(dbContext);
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

// Covers uploaded at runtime are not part of the build-time static asset manifest.
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


await app.RunAsync();
