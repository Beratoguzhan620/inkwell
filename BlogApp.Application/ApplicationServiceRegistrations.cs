using BlogApp.Application.DTOs.Settings;
using BlogApp.Application.Services.Abstract;
using BlogApp.Application.Services.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlogApp.Application;

public static class ApplicationServiceRegistrations
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {



        //Servis'leri uygulamama kaydediyorum
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBlogPostService, BlogPostService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IMailService, SmtpMailService>();

        return services;
    }
}
