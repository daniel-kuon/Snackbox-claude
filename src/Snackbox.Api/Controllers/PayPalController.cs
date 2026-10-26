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
public class PayPalController(ApplicationDbContext context, PayPalSync sync, PayPalMailbox mailbox) : ControllerBase
{
    // --- the mailbox PayPal's mails arrive in (private PayPal accounts) -------------------------

    [HttpGet("mailbox")]
    public ActionResult<PayPalMailboxStatusDto> Mailbox() => Ok(mailbox.GetStatus());

    [HttpPut("mailbox/client-id")]
    public async Task<IActionResult> SetMailboxClientId([FromBody] PayPalMailboxClientIdDto dto)
    {
        try
        {
            await mailbox.SetClientIdAsync(dto.ClientId);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("mailbox/sign-in")]
    public async Task<ActionResult<OneDriveSignInDto>> StartMailboxSignIn()
    {
        try
        {
            return Ok(await mailbox.StartSignInAsync());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>"pending", "connected", "expired" or "failed: ..."</summary>
    [HttpPost("mailbox/sign-in/poll")]
    public async Task<ActionResult<OneDrivePollDto>> PollMailboxSignIn() => Ok(new OneDrivePollDto { Result = await mailbox.PollSignInAsync() });

    [HttpPost("mailbox/disconnect")]
    public async Task<IActionResult> DisconnectMailbox()
    {
        await mailbox.DisconnectAsync();
        return NoContent();
    }

    // --- received payments ------------------------------------------------------------------

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
            Dismissed = r.Transaction.Dismissed,
            ReversedAt = r.Transaction.ReversedAt
        }).ToList();
        return Ok(transactions);
    }

    /// <summary>The payer belongs to this user: books this payment and all their open and future ones.</summary>
    [HttpPost("transactions/{id:int}/assign")]
    public async Task<ActionResult<PayPalStatusDto>> Assign(int id, [FromBody] AssignPayPalPayerDto dto)
    {
        try
        {
            await sync.AssignAsync(id, dto.UserId, dto.Remember);
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
        // A booked one only once PayPal took it back and the admin has dealt with it
        if (transaction.PaymentId != null && transaction.ReversedAt == null) return BadRequest(new { message = "Already booked" });
        transaction.Dismissed = dismissed;
        await context.SaveChangesAsync();
        return NoContent();
    }
}
