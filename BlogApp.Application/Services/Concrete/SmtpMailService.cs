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

        try
        {
            var smtpClient = new SmtpClient(emailSettings.Server, emailSettings.Port)
            {
                Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password),
                EnableSsl = true
            };


            var mailMessage = new MailMessage(emailSettings.Username, to, subject, body)
            {
                IsBodyHtml = true
            };

            await smtpClient.SendMailAsync(mailMessage);

            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail($"Email gönderilirken bir hata oluştu: {e.Message}");
        }

    }
}
