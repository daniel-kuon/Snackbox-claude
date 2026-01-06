namespace Snackbox.Api.Services;

public interface IEmailService
{
    Task SendPaymentReminderAsync(string toEmail, string userName, decimal balance, string? paypalLink = null);
    Task SendBulkPaymentRemindersAsync(IEnumerable<(string email, string userName, decimal balance)> recipients, string? paypalLink = null);
}
