using BlogApp.Application.Services.Abstract;
using BlogApp.Application.Services.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlogApp.Application;

public static class ApplicationServiceRegistrations
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBlogPostService, BlogPostService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IMailService, SmtpMailService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IAdminService, AdminService>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
