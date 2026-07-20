using Landcore.Application.Interfaces;
using Landcore.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Landcore.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(SmtpSettings settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(string toAddress, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            throw new InvalidOperationException("Cannot send an email with no recipient address.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            throw new InvalidOperationException(
                "Smtp:Host is not configured. Set Smtp:Host/Smtp:Username/Smtp:Password via " +
                "`dotnet user-secrets` (dev) or the Smtp__Host/Smtp__Username/Smtp__Password " +
                "environment variables (staging/production) — never commit real credentials. See " +
                "Landcore.Infrastructure/Configuration/SmtpSettings.cs.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };

        using var client = new SmtpClient();
        try
        {
            var socketOptions = _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            _logger.LogInformation("Email sent to {ToAddress} — subject: {Subject}", toAddress, subject);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
