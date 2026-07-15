using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Novacart.Api.Models.Entities;
using MailKit.Net.Smtp;
using MimeKit;

namespace Novacart.Api.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendOrderConfirmationAsync(string email, Order order);
    Task SendOrderStatusUpdateAsync(string email, Order order, string newStatus);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public EmailService(ILogger<EmailService> logger, IWebHostEnvironment env, IConfiguration config)
    {
        _logger = logger;
        _env = env;
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpHost = _config["Smtp:Host"];
        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogInformation($"SMTP Host is not configured. Falling back to log sending. Email to: {to}, Subject: {subject}");
            LogEmailToConsole(to, subject, body);
            return;
        }

        try
        {
            var message = new MimeMessage();
            var fromEmail = _config["Smtp:FromEmail"] ?? "noreply@novacart.local";
            var fromName = _config["Smtp:FromName"] ?? "Novacart Support";
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();
            // Only skip certificate validation when explicitly configured (e.g. local dev SMTP).
            if (_config.GetValue<bool>("Smtp:SkipCertValidation"))
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var portVal = _config["Smtp:Port"];
            int port = int.TryParse(portVal, out var p) ? p : 587;
            bool useSsl = _config.GetValue<bool>("Smtp:EnableSsl");

            await client.ConnectAsync(smtpHost, port, useSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTls);

            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation($"Successfully sent SMTP email to {to} via {smtpHost}:{port}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send SMTP email to {to} via {smtpHost}. Falling back to console logging.");
            LogEmailToConsole(to, subject, body);
        }
    }

    public async Task SendOrderConfirmationAsync(string email, Order order)
    {
        var subject = $"Novacart Order Confirmation #{order.OrderNumber}";
        var body = $"Thank you for your order! Your total is {order.Total:C}. We will notify you once your items ship.";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendOrderStatusUpdateAsync(string email, Order order, string newStatus)
    {
        var subject = $"Your Novacart Order #{order.OrderNumber} status: {newStatus}";
        var body = $"Hello,\n\nThe status of your order #{order.OrderNumber} has been updated to: {newStatus}.\n\nThank you for shopping with Novacart!";
        await SendEmailAsync(email, subject, body);
    }

    private void LogEmailToConsole(string to, string subject, string body)
    {
        _logger.LogInformation("=========================================");
        _logger.LogInformation($"To: {to}");
        _logger.LogInformation($"Subject: {subject}");
        _logger.LogInformation($"Body: {body}");
        _logger.LogInformation("=========================================");
    }
}
