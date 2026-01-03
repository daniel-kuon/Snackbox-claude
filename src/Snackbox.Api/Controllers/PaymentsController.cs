using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
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

        var payment = dto.ToEntity();

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment created: {PaymentId} for user {UserId} - Amount: {Amount}",
            payment.Id, payment.UserId, payment.Amount);

        payment.User = user;
        var resultDto = payment.ToDtoWithUser();

        return CreatedAtAction(nameof(GetByUserId), new { userId = payment.UserId }, resultDto);
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
