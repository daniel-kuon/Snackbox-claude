using System.Net;
using System.Net.Mail;

namespace Snackbox.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly bool _enableSsl;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var emailConfig = _configuration.GetSection("Email");
        _smtpHost = emailConfig["SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost is not configured");
        _smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
        _smtpUsername = emailConfig["SmtpUsername"] ?? throw new InvalidOperationException("Email:SmtpUsername is not configured");
        _smtpPassword = emailConfig["SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword is not configured");
        _fromEmail = emailConfig["FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail is not configured");
        _enableSsl = bool.Parse(emailConfig["EnableSsl"] ?? "true");
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, Stream? attachmentStream = null, string? attachmentFileName = null)
    {
        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            if (attachmentStream != null && attachmentFileName != null)
            {
                var attachment = new Attachment(attachmentStream, attachmentFileName);
                mailMessage.Attachments.Add(attachment);
            }

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }
}
