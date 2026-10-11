using Microsoft.AspNetCore.Authentication.Cookies;

namespace BlogApp.MVC;

public static class MVCServiceRegistrations
{
    public static IServiceCollection AddMVCServices(this IServiceCollection services)
    {
        services.AddControllersWithViews();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                 .AddCookie(options =>
                 {
                     options.Cookie.Name = "auth-cookie"; 
                     options.Cookie.HttpOnly = true; 
                     options.Cookie.IsEssential = true;

                     options.LoginPath = "/Login"; 
                     options.LogoutPath = "/Logout"; 
                     options.AccessDeniedPath = "/AccessDenied"; 

                     options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                     options.SlidingExpiration = true; 
                 });

        return services;
    }
}
