namespace BlogApp.Application.DTOs.Settings;


public class EmailSettings
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;

    /// <summary>Sender address; falls back to <see cref="Username"/>.</summary>
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Inkwell";
}