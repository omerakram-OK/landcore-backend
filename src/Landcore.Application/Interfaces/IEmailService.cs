namespace Landcore.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string toAddress, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
}
