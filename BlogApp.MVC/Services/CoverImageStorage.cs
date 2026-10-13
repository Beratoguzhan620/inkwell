namespace BlogApp.MVC.Services;

/// <summary>
/// Stores cover images under wwwroot/uploads/covers. Files get a random name, so an upload
/// can never overwrite another file or choose its own path.
/// </summary>
public class CoverImageStorage(IWebHostEnvironment environment)
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return null;

        if (file.Length > MaxBytes)
            return "Kapak görseli en fazla 5 MB olabilir.";

        if (!AllowedTypes.ContainsKey(Path.GetExtension(file.FileName)))
            return "Kapak görseli JPG, PNG veya WEBP olmalıdır.";

        return null;
    }

    /// <returns>The public URL of the stored file.</returns>
    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = Path.Combine(environment.WebRootPath, "uploads", "covers");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var stream = File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/covers/{fileName}";
    }
}
