using BlogApp.Application.DTOs.Settings;
using BlogApp.Application.Services.Abstract;
using BlogApp.Core.Results;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace BlogApp.Application.Services.Concrete;

public class SmtpMailService : IMailService
{
    private readonly EmailSettings emailSettings;

    public SmtpMailService(IOptions<EmailSettings> options)
    {
        emailSettings = options.Value;
    }

    public async Task<Result> SendEmailAsync(string to, string subject, string body)
    {

        if (string.IsNullOrWhiteSpace(emailSettings.Server) || string.IsNullOrWhiteSpace(emailSettings.Username))
            return Result.Fail("E-posta ayarları yapılandırılmamış.");

        try
        {
            using var smtpClient = new SmtpClient(emailSettings.Server, emailSettings.Port)
            {
                Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password),
                EnableSsl = emailSettings.EnableSsl,
            };

            var from = new MailAddress(
                string.IsNullOrWhiteSpace(emailSettings.FromAddress) ? emailSettings.Username : emailSettings.FromAddress,
                emailSettings.FromName);

            using var mailMessage = new MailMessage(from, new MailAddress(to))
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            await smtpClient.SendMailAsync(mailMessage);

            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail($"E-posta gönderilemedi: {e.Message}");
        }

    }
}
