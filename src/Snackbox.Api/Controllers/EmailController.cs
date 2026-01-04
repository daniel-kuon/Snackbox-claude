using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class EmailController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        ApplicationDbContext context, 
        IEmailService emailService,
        ILogger<EmailController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("send-payment-reminder/{userId}")]
    public async Task<IActionResult> SendPaymentReminder(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Payments)
            .Include(u => u.Purchases)
                .ThenInclude(p => p.Scans)
            .Include(u => u.Withdrawals)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            return BadRequest(new { message = "User does not have an email address" });
        }

        // Calculate balance (positive = user owes money)
        var totalPaid = user.Payments.Sum(p => p.Amount);
        var totalSpent = user.Purchases
            .Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount));
        var totalWithdrawn = user.Withdrawals.Sum(w => w.Amount);
        var balance = totalSpent - totalPaid + totalWithdrawn;

        if (balance <= 0)
        {
            return BadRequest(new { message = "User does not have an outstanding balance or has a credit" });
        }

        try
        {
            // Get PayPal link from configuration if available
            var paypalLink = HttpContext.RequestServices
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailSettings>>()
                .Value.PayPalLink;

            await _emailService.SendPaymentReminderAsync(user.Email, user.Username, balance, paypalLink);
            
            _logger.LogInformation("Payment reminder sent to user {UserId} ({Email})", userId, user.Email);
            
            return Ok(new { message = "Payment reminder sent successfully", email = user.Email, balance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment reminder to user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to send email", error = ex.Message });
        }
    }

    [HttpPost("send-bulk-payment-reminders")]
    public async Task<IActionResult> SendBulkPaymentReminders([FromBody] BulkReminderRequest? request = null)
    {
        var minBalance = request?.MinimumBalance ?? 10.0m;

        // Get all users with their financial data
        var users = await _context.Users
            .Include(u => u.Payments)
            .Include(u => u.Purchases)
                .ThenInclude(p => p.Scans)
            .Include(u => u.Withdrawals)
            .Where(u => u.Email != null && u.Email != "")
            .ToListAsync();

        var usersToRemind = users
            .Select(u => new
            {
                User = u,
                Balance = u.Purchases
                    .Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount))
                    - u.Payments.Sum(p => p.Amount)
                    + u.Withdrawals.Sum(w => w.Amount)
            })
            .Where(u => u.Balance >= minBalance)
            .ToList();

        if (!usersToRemind.Any())
        {
            return Ok(new { message = "No users found with outstanding balance >= €" + minBalance, count = 0 });
        }

        try
        {
            // Get PayPal link from configuration if available
            var paypalLink = HttpContext.RequestServices
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailSettings>>()
                .Value.PayPalLink;

            var recipients = usersToRemind
                .Select(u => (u.User.Email!, u.User.Username, u.Balance))
                .ToList();

            await _emailService.SendBulkPaymentRemindersAsync(recipients, paypalLink);
            
            _logger.LogInformation("Bulk payment reminders sent to {Count} users", recipients.Count);
            
            return Ok(new 
            { 
                message = $"Payment reminders sent to {recipients.Count} user(s)",
                count = recipients.Count,
                users = usersToRemind.Select(u => new { u.User.Username, u.User.Email, u.Balance })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk payment reminders");
            return StatusCode(500, new { message = "Failed to send emails", error = ex.Message });
        }
    }
}

public class BulkReminderRequest
{
    public decimal MinimumBalance { get; set; } = 10.0m;
}
