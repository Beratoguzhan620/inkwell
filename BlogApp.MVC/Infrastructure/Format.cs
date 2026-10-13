using System.Globalization;

namespace BlogApp.MVC.Infrastructure;

public static class Format
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static string Date(DateTime utc) => utc.ToLocalTime().ToString("d MMMM yyyy", Turkish);

    public static string DateTime(DateTime utc) => utc.ToLocalTime().ToString("d MMMM yyyy, HH:mm", Turkish);

    public static string Initials(string fullName) =>
        string.Concat(fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpper(p[0], Turkish)));
}
