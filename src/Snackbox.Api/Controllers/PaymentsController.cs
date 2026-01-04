using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(ApplicationDbContext context, ILogger<PaymentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll()
    {
        var payments = await _context.Payments
            .Include(p => p.User)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

        return Ok(payments.ToDtoListWithUser());
    }

    [HttpGet("my-payments")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetMyPayments()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var payments = await _context.Payments
            .Include(p => p.User)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

        return Ok(payments.ToDtoListWithUser());
    }

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetByUserId(int userId)
    {
        var payments = await _context.Payments
            .Include(p => p.User)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

        return Ok(payments.ToDtoListWithUser());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Payment amount must be greater than zero" });
        }

        // Validate PayPal payment has admin user
        if (dto.Type == "PayPal" && dto.AdminUserId == null)
        {
            return BadRequest(new { message = "PayPal payment requires an admin user" });
        }

        // Validate admin user exists if specified
        User? adminUser = null;
        if (dto.AdminUserId.HasValue)
        {
            adminUser = await _context.Users.FindAsync(dto.AdminUserId.Value);
            if (adminUser == null)
            {
                return BadRequest(new { message = "Admin user not found" });
            }
            if (!adminUser.IsAdmin)
            {
                return BadRequest(new { message = "Specified user is not an admin" });
            }
        }

        var payment = dto.ToEntity();
        
        // For PayPal payments, create a linked withdrawal for the admin
        Withdrawal? withdrawal = null;
        if (payment.Type == PaymentType.PayPal && adminUser != null)
        {
            withdrawal = new Withdrawal
            {
                UserId = adminUser.Id,
                Amount = payment.Amount,
                WithdrawnAt = DateTime.UtcNow,
                Notes = $"PayPal payment from {user.Username}"
            };
            _context.Withdrawals.Add(withdrawal);
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Link the payment and withdrawal
        if (withdrawal != null)
        {
            payment.LinkedWithdrawalId = withdrawal.Id;
            withdrawal.LinkedPaymentId = payment.Id;
            await _context.SaveChangesAsync();
        }

        // Update cash register balance for cash register payments
        if (payment.Type == PaymentType.CashRegister)
        {
            await UpdateCashRegister(payment.Amount, user.Id);
        }

        _logger.LogInformation("Payment created: {PaymentId} for user {UserId} - Amount: {Amount}, Type: {Type}",
            payment.Id, payment.UserId, payment.Amount, payment.Type);

        payment.User = user;
        payment.AdminUser = adminUser;
        var resultDto = payment.ToDtoWithUser();

        return CreatedAtAction(nameof(GetByUserId), new { userId = payment.UserId }, resultDto);
    }

    private async Task UpdateCashRegister(decimal amount, int userId)
    {
        var cashRegister = await _context.CashRegister.FirstOrDefaultAsync();
        if (cashRegister == null)
        {
            cashRegister = new CashRegister
            {
                CurrentBalance = amount,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedByUserId = userId
            };
            _context.CashRegister.Add(cashRegister);
        }
        else
        {
            cashRegister.CurrentBalance += amount;
            cashRegister.LastUpdatedAt = DateTime.UtcNow;
            cashRegister.LastUpdatedByUserId = userId;
        }
        await _context.SaveChangesAsync();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var payment = await _context.Payments.FindAsync(id);

        if (payment == null)
        {
            return NotFound(new { message = "Payment not found" });
        }

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment deleted: {PaymentId}", payment.Id);

        return NoContent();
    }
}
