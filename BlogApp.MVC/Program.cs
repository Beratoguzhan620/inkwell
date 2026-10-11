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

//IOptions pattern ile configuration'daki ayarları ilgili class'a bağlıyorum
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//Seed database
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//await dbContext.Database.MigrateAsync(); //update-database ile yapmak yerine bu satırı açarsak otomatik olarak her çalışmada migraiton'lar db'ye uygulanır
await DbSeeder.SeedAsync(dbContext);

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
