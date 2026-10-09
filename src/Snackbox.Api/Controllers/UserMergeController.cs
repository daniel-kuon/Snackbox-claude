using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;

namespace Snackbox.Api.Controllers;

/// <summary>
/// Someone lost their card and set up a new one: the new card's user is folded into their
/// existing account - barcodes, purchases, payments and the rest move over, the card's user is
/// deleted. The new card's codes then buy on the existing account.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UserMergeController(ApplicationDbContext context, ILogger<UserMergeController> logger) : ControllerBase
{
    [HttpPost("{id:int}/merge-into/{targetId:int}")]
    public async Task<ActionResult> MergeInto(int id, int targetId)
    {
        if (id == targetId) return BadRequest(new { message = "A user cannot be merged into itself" });

        var source = await context.Users.FindAsync(id);
        var target = await context.Users.FindAsync(targetId);
        if (source == null || target == null) return NotFound(new { message = "User not found" });

        // An admin account is not a card - folding one away by mistake would lock its owner out
        if (source.IsAdmin) return BadRequest(new { message = "Admin accounts cannot be merged into another user" });

        // Two saves (see the card number below), so a transaction - where there is a real database
        await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync() : null;

        foreach (var barcode in await context.Barcodes.Where(b => b.UserId == id).ToListAsync()) barcode.UserId = targetId;
        foreach (var purchase in await context.Purchases.Where(p => p.UserId == id).ToListAsync()) purchase.UserId = targetId;
        foreach (var payment in await context.Payments.Where(p => p.UserId == id).ToListAsync()) payment.UserId = targetId;
        foreach (var payment in await context.Payments.Where(p => p.AdminUserId == id).ToListAsync()) payment.AdminUserId = targetId;
        foreach (var deposit in await context.Deposits.Where(d => d.UserId == id).ToListAsync()) deposit.UserId = targetId;
        foreach (var withdrawal in await context.Withdrawals.Where(w => w.UserId == id).ToListAsync()) withdrawal.UserId = targetId;
        foreach (var invoice in await context.Invoices.Where(i => i.PaidByUserId == id).ToListAsync()) invoice.PaidByUserId = targetId;
        foreach (var invoice in await context.Invoices.Where(i => i.CreatedByUserId == id).ToListAsync()) invoice.CreatedByUserId = targetId;
        foreach (var register in await context.CashRegister.Where(c => c.LastUpdatedByUserId == id).ToListAsync()) register.LastUpdatedByUserId = targetId;

        // Achievements are once per user and intro steps once per step: keep the target's,
        // take over only what it does not have yet
        var targetAchievements = await context.UserAchievements.Where(a => a.UserId == targetId).Select(a => a.AchievementId).ToListAsync();
        foreach (var achievement in await context.UserAchievements.Where(a => a.UserId == id).ToListAsync())
        {
            if (targetAchievements.Contains(achievement.AchievementId)) context.UserAchievements.Remove(achievement);
            else achievement.UserId = targetId;
        }

        var targetSteps = await context.UserWizardSteps.Where(s => s.UserId == targetId).Select(s => s.StepKey).ToListAsync();
        foreach (var step in await context.UserWizardSteps.Where(s => s.UserId == id).ToListAsync())
        {
            if (targetSteps.Contains(step.StepKey)) context.UserWizardSteps.Remove(step);
            else step.UserId = targetId;
        }

        // The card number goes with the card, so the card wizard still shows it as taken.
        // Freed on the source first: the number is unique, and both rows change in this save.
        var cardNumber = source.CardNumber;
        source.CardNumber = null;
        await context.SaveChangesAsync();
        if (cardNumber != null) target.CardNumber = cardNumber;

        context.Users.Remove(source);
        await context.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();

        logger.LogInformation("Merged user {SourceId} ({SourceName}) into {TargetId} ({TargetName})",
                              id, source.Username, targetId, target.Username);

        return Ok(new { userId = targetId });
    }
}
