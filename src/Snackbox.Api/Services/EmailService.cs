using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Snackbox.Api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendPaymentReminderAsync(string toEmail, string userName, decimal balance, string? paypalLink = null)
    {
        if (string.IsNullOrEmpty(toEmail))
        {
            _logger.LogWarning("Cannot send email to user {UserName} - no email address provided", userName);
            return;
        }

        if (!_emailSettings.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Would have sent payment reminder to {Email}", toEmail);
            return;
        }

        var subject = "Payment Reminder - Snackbox";
        var body = BuildPaymentReminderEmail(userName, balance, paypalLink);

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendBulkPaymentRemindersAsync(
        IEnumerable<(string email, string userName, decimal balance)> recipients, 
        string? paypalLink = null)
    {
        var tasks = recipients
            .Where(r => !string.IsNullOrEmpty(r.email))
            .Select(r => SendPaymentReminderAsync(r.email, r.userName, r.balance, paypalLink));

        await Task.WhenAll(tasks);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
            client.EnableSsl = _emailSettings.EnableSsl;
            client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    private string BuildPaymentReminderEmail(string userName, decimal balance, string? paypalLink)
    {
        var paypalSection = string.IsNullOrEmpty(paypalLink)
            ? ""
            : $@"
                <p>You can make a payment via PayPal using the link below:</p>
                <p style='margin: 20px 0;'>
                    <a href='{paypalLink}' style='background-color: #0070ba; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>
                        Pay with PayPal
                    </a>
                </p>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Payment Reminder</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
        <h2 style='color: #d9534f; margin-top: 0;'>Payment Reminder</h2>
    </div>
    
    <p>Hello {userName},</p>
    
    <p>This is a friendly reminder that you currently have an outstanding balance on your Snackbox account:</p>
    
    <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;'>
        <strong style='font-size: 18px; color: #d9534f;'>Outstanding Balance: €{balance:F2}</strong>
    </div>
    
    <p>Please arrange payment at your earliest convenience.</p>
    {paypalSection}
    <p>If you have any questions or concerns, please don't hesitate to contact an administrator.</p>
    
    <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
    
    <p style='color: #666; font-size: 12px;'>
        This is an automated message from the Snackbox system. Please do not reply to this email.
    </p>
</body>
</html>";
    }
}

public class EmailSettings
{
    public bool Enabled { get; set; }
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Snackbox";
    public string? PayPalLink { get; set; }
}
