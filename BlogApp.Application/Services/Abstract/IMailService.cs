using BlogApp.Core.Results;

namespace BlogApp.Application.Services.Abstract;

public interface IMailService
{
    Task<Result> SendEmailAsync(string to, string subject, string body);
}
