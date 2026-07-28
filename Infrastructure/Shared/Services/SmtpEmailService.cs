using System;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Interfaces;
using Shared.Models;

namespace Shared.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.User));
            message.To.Add(new MailboxAddress(string.Empty, to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.User, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            await SaveToDevFolder(to, subject, body);
        }
    }

    private async Task SaveToDevFolder(string to, string subject, string body)
    {
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "sent-emails");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.html";
        var filePath = Path.Combine(folder, fileName);

        var content = $"""
            To: {to}
            Subject: {subject}
            Date: {DateTime.UtcNow:O}
            ---
            {body}
            """;

        await File.WriteAllTextAsync(filePath, content);
        _logger.LogWarning("Email saved to dev folder: {Path}", filePath);
    }
}
