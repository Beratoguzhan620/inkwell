using BlogApp.MVC.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace BlogApp.MVC;

public static class MVCServiceRegistrations
{
    public static IServiceCollection AddMVCServices(this IServiceCollection services)
    {
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            options.ModelMetadataDetailsProviders.Add(new TurkishValidationMetadataProvider());

            var messages = options.ModelBindingMessageProvider;
            messages.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
            messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"'{value}' {field} için geçerli bir değer değil.");
            messages.SetValueIsInvalidAccessor(value => $"'{value}' geçerli bir değer değil.");
            messages.SetValueMustBeANumberAccessor(field => $"{field} bir sayı olmalıdır.");
            messages.SetMissingBindRequiredValueAccessor(field => $"{field} alanı zorunludur.");
        });

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                 .AddCookie(options =>
                 {
                     options.Cookie.Name = "auth-cookie"; 
                     options.Cookie.HttpOnly = true; 
                     options.Cookie.IsEssential = true;

                     options.LoginPath = "/Login"; 
                     options.LogoutPath = "/Logout"; 
                     options.AccessDeniedPath = "/not-found";

                     options.ExpireTimeSpan = TimeSpan.FromHours(3);
                     options.SlidingExpiration = true; 
                 });

        return services;
    }
}
