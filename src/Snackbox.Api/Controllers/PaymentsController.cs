using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;
using Snackbox.Api.Telemetry;

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
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetAllPayments");
        
        _logger.LogInformation("Admin fetching all payments");

        var payments = await _context.Payments
            .Include(p => p.User)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Username = p.User.Username,
                Amount = p.Amount,
                PaidAt = p.PaidAt,
                Notes = p.Notes
            })
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved all payments. Count: {PaymentCount}, TotalAmount: {TotalAmount}",
            payments.Count,
            payments.Sum(p => p.Amount));

        activity?.SetTag("payments.count", payments.Count);
        activity?.SetTag("payments.total_amount", payments.Sum(p => p.Amount));

        return Ok(payments);
    }

    [HttpGet("my-payments")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetMyPayments()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetMyPayments");
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogWarning("Unauthorized access attempt to GetMyPayments");
            return Unauthorized();
        }

        activity?.SetTag("user.id", userId);
        _logger.LogInformation("Fetching payments for user. UserId: {UserId}", userId);

        var payments = await _context.Payments
            .Include(p => p.User)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Username = p.User.Username,
                Amount = p.Amount,
                PaidAt = p.PaidAt,
                Notes = p.Notes
            })
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved payments for user. UserId: {UserId}, Count: {PaymentCount}, TotalAmount: {TotalAmount}",
            userId,
            payments.Count,
            payments.Sum(p => p.Amount));

        activity?.SetTag("payments.count", payments.Count);
        activity?.SetTag("payments.total_amount", payments.Sum(p => p.Amount));

        return Ok(payments);
    }

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetByUserId(int userId)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetPaymentsByUserId");
        activity?.SetTag("user.id", userId);
        
        _logger.LogInformation("Admin fetching payments for user. UserId: {UserId}", userId);

        var payments = await _context.Payments
            .Include(p => p.User)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Username = p.User.Username,
                Amount = p.Amount,
                PaidAt = p.PaidAt,
                Notes = p.Notes
            })
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved payments for user. UserId: {UserId}, Count: {PaymentCount}, TotalAmount: {TotalAmount}",
            userId,
            payments.Count,
            payments.Sum(p => p.Amount));

        activity?.SetTag("payments.count", payments.Count);
        activity?.SetTag("payments.total_amount", payments.Sum(p => p.Amount));

        return Ok(payments);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("CreatePayment");
        activity?.SetTag("user.id", dto.UserId);
        activity?.SetTag("payment.amount", dto.Amount);
        
        _logger.LogInformation(
            "Creating payment. UserId: {UserId}, Amount: {Amount}, Notes: {Notes}",
            dto.UserId,
            dto.Amount,
            dto.Notes ?? "N/A");

        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            _logger.LogWarning("Payment creation failed: User not found. UserId: {UserId}", dto.UserId);
            activity?.SetStatus(ActivityStatusCode.Error, "User not found");
            return BadRequest(new { message = "User not found" });
        }

        if (dto.Amount <= 0)
        {
            _logger.LogWarning(
                "Payment creation failed: Invalid amount. UserId: {UserId}, Amount: {Amount}",
                dto.UserId,
                dto.Amount);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid amount");
            return BadRequest(new { message = "Payment amount must be greater than zero" });
        }

        var payment = new Payment
        {
            UserId = dto.UserId,
            Amount = dto.Amount,
            PaidAt = DateTime.UtcNow,
            Notes = dto.Notes
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Payment created successfully. PaymentId: {PaymentId}, UserId: {UserId}, Username: {Username}, Amount: {Amount}",
            payment.Id,
            payment.UserId,
            user.Username,
            payment.Amount);

        SnackboxTelemetry.PaymentCounter.Add(1,
            new KeyValuePair<string, object?>("user.id", payment.UserId));
        SnackboxTelemetry.PaymentAmountHistogram.Record((double)payment.Amount,
            new KeyValuePair<string, object?>("user.id", payment.UserId));

        activity?.SetTag("payment.id", payment.Id);

        var resultDto = new PaymentDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            Username = user.Username,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt,
            Notes = payment.Notes
        };

        return CreatedAtAction(nameof(GetByUserId), new { userId = payment.UserId }, resultDto);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("DeletePayment");
        activity?.SetTag("payment.id", id);
        
        _logger.LogInformation("Attempting to delete payment. PaymentId: {PaymentId}", id);

        var payment = await _context.Payments.FindAsync(id);

        if (payment == null)
        {
            _logger.LogWarning("Payment deletion failed: Payment not found. PaymentId: {PaymentId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Payment not found");
            return NotFound(new { message = "Payment not found" });
        }

        var userId = payment.UserId;
        var amount = payment.Amount;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Payment deleted successfully. PaymentId: {PaymentId}, UserId: {UserId}, Amount: {Amount}",
            id,
            userId,
            amount);

        activity?.SetTag("payment.user_id", userId);
        activity?.SetTag("payment.amount", amount);

        return NoContent();
    }
}
