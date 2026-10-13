using System.Globalization;
using System.Text;

namespace BlogApp.Application.Text;

public static class SlugGenerator
{
    private const int MaxLength = 80;

    /// <summary>"Çalışan Yazılım Ekipleri" → "calisan-yazilim-ekipleri".</summary>
    public static string Generate(string title)
    {
        var lower = title.Trim().ToLower(CultureInfo.GetCultureInfo("tr-TR"));
        var builder = new StringBuilder(lower.Length);

        foreach (var ch in lower)
        {
            var mapped = ch switch
            {
                'ç' => "c",
                'ğ' => "g",
                'ı' => "i",
                'ö' => "o",
                'ş' => "s",
                'ü' => "u",
                'â' => "a",
                'î' => "i",
                'û' => "u",
                _ when char.IsAsciiLetterOrDigit(ch) => ch.ToString(),
                _ => "-",
            };
            builder.Append(mapped);
        }

        var slug = string.Join('-', builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));

        if (slug.Length > MaxLength)
            slug = slug[..MaxLength].TrimEnd('-');

        return slug.Length == 0 ? "yazi" : slug;
    }
}
