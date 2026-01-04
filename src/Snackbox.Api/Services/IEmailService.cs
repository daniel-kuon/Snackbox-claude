namespace Snackbox.Api.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends an email with an attachment
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (supports HTML)</param>
    /// <param name="attachmentStream">Optional attachment stream</param>
    /// <param name="attachmentFileName">Optional attachment filename</param>
    Task SendEmailAsync(string toEmail, string subject, string body, Stream? attachmentStream = null, string? attachmentFileName = null);
}
