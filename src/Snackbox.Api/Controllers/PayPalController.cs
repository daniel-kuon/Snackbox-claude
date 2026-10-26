using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

/// <summary>Received PayPal payments with "Snackbox" in the note (Admin → PayPal), see <see cref="PayPalSync"/>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class PayPalController(ApplicationDbContext context, PayPalSync sync) : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<PayPalStatusDto> Status() => Ok(sync.GetStatus());

    [HttpPost("sync")]
    public async Task<ActionResult<PayPalStatusDto>> Sync(CancellationToken cancellationToken) =>
        Ok(await sync.SyncAsync(cancellationToken));

    /// <summary>Newest first: open ones to assign, booked ones for reference.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<PayPalTransactionDto>>> Transactions([FromQuery] int count = 200)
    {
        var payers = await context.PayPalPayers.ToDictionaryAsync(p => p.PayerKey, p => p.UserId);
        var rows = await context.PayPalTransactions
            .AsNoTracking()
            .OrderByDescending(t => t.ReceivedAt)
            .Take(Math.Clamp(count, 1, 1000))
            .Select(t => new { Transaction = t, BookedUsername = t.Payment != null ? t.Payment.User.Username : null })
            .ToListAsync();

        var transactions = rows.Select(r => new PayPalTransactionDto
        {
            Id = r.Transaction.Id,
            TransactionId = r.Transaction.TransactionId,
            ReceivedAt = r.Transaction.ReceivedAt,
            Amount = r.Transaction.Amount,
            Currency = r.Transaction.Currency,
            PayerName = r.Transaction.PayerName,
            PayerEmail = r.Transaction.PayerEmail,
            Note = r.Transaction.Note,
            PaymentId = r.Transaction.PaymentId,
            BookedUsername = r.BookedUsername,
            AssignedUserId = payers.TryGetValue(r.Transaction.PayerKey, out var userId) ? userId : null,
            Dismissed = r.Transaction.Dismissed
        }).ToList();
        return Ok(transactions);
    }

    /// <summary>The payer belongs to this user: books this payment and all their open and future ones.</summary>
    [HttpPost("transactions/{id:int}/assign")]
    public async Task<ActionResult<PayPalStatusDto>> Assign(int id, [FromBody] AssignPayPalPayerDto dto)
    {
        try
        {
            await sync.AssignAsync(id, dto.UserId);
            return Ok(sync.GetStatus());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (PayPalException ex)
        {
            // The payer is assigned; booking waits for the receiving admin to be set
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Not for Snackbox after all, or booked by hand: off the open list.</summary>
    [HttpPost("transactions/{id:int}/dismiss")]
    public async Task<IActionResult> Dismiss(int id, [FromQuery] bool dismissed = true)
    {
        var transaction = await context.PayPalTransactions.FindAsync(id);
        if (transaction == null) return NotFound();
        if (transaction.PaymentId != null) return BadRequest(new { message = "Already booked" });
        transaction.Dismissed = dismissed;
        await context.SaveChangesAsync();
        return NoContent();
    }
}
