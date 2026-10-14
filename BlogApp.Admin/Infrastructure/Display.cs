using System.Globalization;
using BlogApp.Data.Constants;
using BlogApp.Data.Entities;

namespace BlogApp.Admin.Infrastructure;

public static class Display
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static string Date(DateTime value) => value.ToLocalTime().ToString("d MMM yyyy", Turkish);

    public static string DateTime(DateTime value) => value.ToLocalTime().ToString("d MMM yyyy, HH:mm", Turkish);

    public static string RoleLabel(string role) => role switch
    {
        RoleConstants.Admin => "Yönetici",
        RoleConstants.Author => "Yazar",
        RoleConstants.Reader => "Okur",
        _ => "Rolsüz",
    };

    public static (string Text, string Css) Status(BlogPostStatus status) => status switch
    {
        BlogPostStatus.Approved => ("Yayında", "badge-success"),
        BlogPostStatus.Rejected => ("Kaldırıldı", "badge-danger"),
        _ => ("Taslak", "badge-neutral"),
    };
}
