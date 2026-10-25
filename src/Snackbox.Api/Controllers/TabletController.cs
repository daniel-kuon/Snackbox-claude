using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

/// <summary>
/// The tablet next to the scanner. It shows what is on the shelf; once a user who picks on the
/// tablet scans their card it books what they tap into that purchase - one scan row per item, at
/// the product's price, under the card code they scanned. Anonymous like the kiosk's scan: it can
/// only ever add to the session a card scan just opened.
/// </summary>
[ApiController]
[Route("api/tablet")]
[AllowAnonymous]
public class TabletController(
    ApplicationDbContext context,
    TabletSessionStore sessions,
    IStockCalculationService stockCalculation,
    IAchievementService achievementService,
    IProductBestBeforeDateService bestBeforeDates,
    TabletDisplay display) : ControllerBase
{
    /// <summary>Recorded in user_wizard_steps once someone went through the tablet's introduction.</summary>
    public const string IntroStepKey = "tablet_intro";

    /// <summary>Products with a price that are on the shelf.</summary>
    [HttpGet("products")]
    public async Task<ActionResult<List<TabletProductDto>>> GetProducts()
    {
        var products = await context.Products
            .AsNoTracking()
            .Where(p => p.Price != null)
            .Include(p => p.Batches).ThenInclude(b => b.ShelvingActions)
            .ToListAsync();
        // Only the two columns - not the picture bytes
        var imageVersions = await context.ProductImages
            .Select(i => new { i.ProductId, i.UpdatedAt })
            .ToDictionaryAsync(i => i.ProductId, i => i.UpdatedAt.Ticks);

        return Ok(products
            .Where(p => p.Batches.Sum(b => stockCalculation.CalculateShelfQuantity(b.ShelvingActions)) > 0)
            .OrderBy(p => p.Name)
            .Select(p => new TabletProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price!.Value,
                ShelfSlot = p.ShelfSlot,
                HasImage = imageVersions.ContainsKey(p.Id),
                ImageVersion = imageVersions.GetValueOrDefault(p.Id)
            })
            .ToList());
    }

    [HttpGet("state")]
    public async Task<ActionResult<TabletStateDto>> GetState()
    {
        var session = sessions.Current;
        if (session == null) return Ok(new TabletStateDto());

        return Ok(await BuildStateAsync(session));
    }

    [HttpPost("items/{productId:int}")]
    public async Task<ActionResult<TabletStateDto>> AddItem(int productId)
    {
        var session = sessions.Current;
        if (session == null) return Conflict(new { message = "Scan your card first" });
        if (!session.Picking) return Conflict(new { message = "You pay with your card codes" });

        var product = await context.Products.FindAsync(productId);
        if (product?.Price == null) return NotFound(new { message = "Product not available" });

        var now = DateTime.UtcNow;
        if (session.PurchaseId == null)
        {
            // After Done only through New purchase (which reopens the session)
            if (session.Completed) return Conflict(new { message = "Tap New purchase first" });

            // The first pick after New purchase: a new purchase
            var purchase = new Purchase { UserId = session.UserId, CreatedAt = now, UpdatedAt = now };
            context.Purchases.Add(purchase);
            await context.SaveChangesAsync();
            sessions.StartNewPurchase(purchase.Id);
            session = sessions.Current ?? session with { PurchaseId = purchase.Id };
        }

        var scan = new BarcodeScan
        {
            PurchaseId = session.PurchaseId!.Value,
            BarcodeId = session.BarcodeId,
            ProductId = product.Id,
            Amount = product.Price.Value,
            ScannedAt = now,
            TraceId = Activity.Current?.TraceId.ToHexString()
        };
        context.BarcodeScans.Add(scan);
        await TakeOffShelfAsync(product.Id, scan, now);
        await TouchPurchaseAsync(session, now);
        await bestBeforeDates.UpdateProductBestBeforeDatesAsync(product.Id);

        await achievementService.CheckImmediateAchievementsAsync(session.UserId, session.PurchaseId.Value);

        return Ok(await BuildStateAsync(sessions.Current ?? session));
    }

    /// <summary>One item less, or the product's line removed completely with <paramref name="all"/>.</summary>
    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<TabletStateDto>> RemoveItem(int productId, [FromQuery] bool all = false)
    {
        var session = sessions.Current;
        if (session == null) return Conflict(new { message = "Scan your card first" });
        if (!session.Picking) return Conflict(new { message = "You pay with your card codes" });
        if (session.PurchaseId == null) return Ok(await BuildStateAsync(session));

        var scans = await context.BarcodeScans
            .Where(s => s.PurchaseId == session.PurchaseId && s.ProductId == productId)
            .OrderByDescending(s => s.ScannedAt)
            .ToListAsync();
        var removed = (all ? scans : scans.Take(1)).ToList();
        var removedIds = removed.Select(s => s.Id).ToList();

        // Back on the shelf. The database cascades this too; done here so it does not depend on it.
        context.ShelvingActions.RemoveRange(await context.ShelvingActions.Where(a => a.BarcodeScanId != null && removedIds.Contains(a.BarcodeScanId.Value)).ToListAsync());
        context.BarcodeScans.RemoveRange(removed);
        await TouchPurchaseAsync(session, DateTime.UtcNow);
        await bestBeforeDates.UpdateProductBestBeforeDatesAsync(productId);

        return Ok(await BuildStateAsync(sessions.Current ?? session));
    }

    /// <summary>
    /// Done: the purchase is closed right away (achievements included - the tablet shows them
    /// next), and the next scan starts a new one. The tablet stays on a few seconds: a pick
    /// starts a new purchase, the toolbar still works; then it goes dark.
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<TabletStateDto>> Complete()
    {
        var session = sessions.Current;
        if (session == null) return Ok(new TabletStateDto());

        if (session.PurchaseId is { } purchaseId)
        {
            var purchase = await context.Purchases.Include(p => p.Scans).FirstOrDefaultAsync(p => p.Id == purchaseId);
            if (purchase != null && purchase.Scans.Count > 0)
            {
                purchase.UpdatedAt = purchase.Scans.Max(s => s.ScannedAt);
                await context.SaveChangesAsync();
                await achievementService.CheckAndAwardAchievementsAsync(session.UserId, purchaseId);
            }
            else if (purchase is { ManualAmount: null })
            {
                // Opened by a scan, nothing picked: nothing to keep
                context.Purchases.Remove(purchase);
                await context.SaveChangesAsync();
            }
        }

        sessions.Complete(session.PurchaseId);
        return Ok(await BuildStateAsync(sessions.Current ?? session));
    }

    /// <summary>*New purchase* on the summary after Done - only in the seconds right after it.</summary>
    [HttpPost("new-purchase")]
    public async Task<ActionResult<TabletStateDto>> NewPurchase()
    {
        var session = sessions.Current;
        if (session == null || !session.Picking) return Conflict(new { message = "Scan your card first" });
        if (session.Completed)
        {
            if (!sessions.MayStartNewPurchase(session)) return Conflict(new { message = "Scan your card to start a new purchase" });
            sessions.Reopen();
        }
        return Ok(await BuildStateAsync(sessions.Current ?? session));
    }

    /// <summary>
    /// After Done: what was just bought, the month's last purchases and the last payment - of the
    /// session's user only.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<TabletSummaryDto>> Summary()
    {
        var session = sessions.Current;
        if (session == null) return Ok(new TabletSummaryDto());

        var summary = new TabletSummaryDto();
        if (session.ClosedPurchaseId is { } closedId)
        {
            var scans = await context.BarcodeScans
                .AsNoTracking()
                .Where(s => s.PurchaseId == closedId)
                .Select(s => new { s.ProductId, ProductName = s.Product != null ? s.Product.Name : null, s.Amount })
                .ToListAsync();
            summary.Items = scans
                .GroupBy(s => new { s.ProductId, s.ProductName, s.Amount })
                .Select(g => new TabletItemDto
                {
                    ProductId = g.Key.ProductId ?? 0,
                    Name = g.Key.ProductName ?? $"Card code {g.Key.Amount:0.00} €",
                    UnitPrice = g.Key.Amount,
                    Quantity = g.Count()
                })
                .ToList();
            var discounts = await context.PurchaseDiscounts.Where(pd => pd.PurchaseId == closedId).SumAsync(pd => pd.DiscountAmount);
            summary.Total = Math.Max(0, scans.Sum(s => s.Amount) - discounts);
        }

        // Same total rule as the purchase history: ManualAmount, otherwise scans minus discounts
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        summary.RecentPurchases = await context.Purchases
            .AsNoTracking()
            .Where(p => p.UserId == session.UserId && p.UpdatedAt >= monthStart && (p.ManualAmount != null || p.Scans.Any()))
            .OrderByDescending(p => p.UpdatedAt)
            .Take(5)
            .Select(p => new TabletPurchaseLineDto
            {
                At = p.UpdatedAt,
                Total = p.ManualAmount ?? p.Scans.Sum(s => s.Amount) - p.AppliedDiscounts.Sum(pd => pd.DiscountAmount),
                ItemCount = p.Scans.Count
            })
            .ToListAsync();

        summary.LastPayment = await context.Payments
            .AsNoTracking()
            // Money paid in: not the import's balance correction, not a refund or reversal
            .Where(p => p.UserId == session.UserId && p.Amount > 0 && p.Notes != Services.LegacyImport.LegacyImportService.BalanceAnchorNote)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new TabletPaymentLineDto { At = p.PaidAt, Amount = p.Amount })
            .FirstOrDefaultAsync();

        return Ok(summary);
    }

    /// <summary>
    /// Introduction or a dialog open: the countdown stops (2 minutes without a touch end the
    /// session all the same); closed again: it runs from the full timeout. Also a touch while open.
    /// </summary>
    [HttpPost("pause")]
    public ActionResult Pause([FromQuery] bool paused)
    {
        sessions.SetPaused(paused);
        return NoContent();
    }

    /// <summary>Achievements earned but not seen yet - handed out once.</summary>
    [HttpPost("achievements/claim")]
    public async Task<ActionResult<List<AchievementDto>>> ClaimAchievements()
    {
        var session = sessions.Current;
        if (session == null) return Ok(new List<AchievementDto>());
        return Ok(await UnshownAchievements.ClaimAsync(context, session.UserId));
    }

    /// <summary>Everything the user earned, newest first (toolbar).</summary>
    [HttpGet("achievements")]
    public async Task<ActionResult<List<AchievementDto>>> GetAchievements()
    {
        var session = sessions.Current;
        if (session == null) return Ok(new List<AchievementDto>());

        return Ok(await context.UserAchievements
            .Where(ua => ua.UserId == session.UserId)
            .OrderByDescending(ua => ua.EarnedAt)
            .Select(ua => new AchievementDto
            {
                Id = ua.Achievement.Id,
                Code = ua.Achievement.Code,
                Name = ua.Achievement.Name,
                Description = ua.Achievement.Description,
                Category = ua.Achievement.Category.ToString(),
                ImageUrl = ua.Achievement.ImageUrl,
                EarnedAt = ua.EarnedAt
            })
            .ToListAsync());
    }

    /// <summary>The session ran out (no Done): the tablet asks to go dark. Ignored while someone uses it.</summary>
    [HttpPost("sleep")]
    public ActionResult Sleep()
    {
        if (sessions.Current == null) display.Sleep();
        return NoContent();
    }

    [HttpPost("intro-seen")]
    public async Task<ActionResult> IntroSeen()
    {
        var session = sessions.Current;
        if (session == null) return Conflict(new { message = "Scan your card first" });

        if (!await context.UserWizardSteps.AnyAsync(s => s.UserId == session.UserId && s.StepKey == IntroStepKey))
        {
            context.UserWizardSteps.Add(new UserWizardStep { UserId = session.UserId, StepKey = IntroStepKey, SeenAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }
        return NoContent();
    }

    /// <summary>
    /// Unlike card scans, a tablet pick says which product left the shelf: one item comes off
    /// the batch with the oldest best-before date that still has some on the shelf. Nothing on
    /// the shelf (the count is off) books nothing - the inventory count fixes the shelf later.
    /// </summary>
    private async Task TakeOffShelfAsync(int productId, BarcodeScan scan, DateTime now)
    {
        var batches = await context.ProductBatches
            .Include(b => b.ShelvingActions)
            .Where(b => b.ProductId == productId)
            .OrderBy(b => b.BestBeforeDate)
            .ToListAsync();
        var batch = batches.FirstOrDefault(b => stockCalculation.CalculateShelfQuantity(b.ShelvingActions) > 0);
        if (batch == null) return;

        context.ShelvingActions.Add(new ShelvingAction
        {
            ProductBatchId = batch.Id,
            Quantity = 1,
            Type = ShelvingActionType.RemovedFromShelf,
            ActionAt = now,
            BarcodeScan = scan
        });
    }

    private async Task TouchPurchaseAsync(TabletSessionStore.Session session, DateTime now)
    {
        var purchaseId = session.PurchaseId!.Value;
        var purchase = await context.Purchases.FindAsync(purchaseId);
        if (purchase != null) purchase.UpdatedAt = now;
        await context.SaveChangesAsync();

        var total = await context.BarcodeScans.Where(s => s.PurchaseId == purchaseId).SumAsync(s => s.Amount);
        await PurchaseDiscountCalculator.ApplyAsync(context, purchaseId, total);
        sessions.Touch();
    }

    private async Task<TabletStateDto> BuildStateAsync(TabletSessionStore.Session session)
    {
        var scans = await context.BarcodeScans
            .AsNoTracking()
            .Where(s => s.PurchaseId == session.PurchaseId)
            .Select(s => new { s.ProductId, ProductName = s.Product != null ? s.Product.Name : null, s.Amount })
            .ToListAsync();
        var discounts = await context.PurchaseDiscounts
            .Where(pd => pd.PurchaseId == session.PurchaseId)
            .SumAsync(pd => pd.DiscountAmount);

        var total = scans.Sum(s => s.Amount);
        return new TabletStateDto
        {
            Active = true,
            Completed = session.Completed,
            Paused = session.Paused,
            Picking = session.Picking,
            UserId = session.UserId,
            Username = session.Username,
            CardScans = scans
                .Where(s => s.ProductId == null)
                .GroupBy(s => s.Amount)
                .Select(g => new TabletCardScanDto { Amount = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Amount)
                .ToList(),
            OpenAmount = await UserBalance.OpenAmountAsync(context, session.UserId),
            ShowIntro = !await context.UserWizardSteps.AnyAsync(s => s.UserId == session.UserId && s.StepKey == IntroStepKey),
            Items = scans
                .Where(s => s.ProductId != null)
                .GroupBy(s => new { s.ProductId, s.ProductName, s.Amount })
                .Select(g => new TabletItemDto { ProductId = g.Key.ProductId!.Value, Name = g.Key.ProductName ?? "?", UnitPrice = g.Key.Amount, Quantity = g.Count() })
                .ToList(),
            Total = total,
            DiscountedTotal = Math.Max(0, total - discounts),
            // Paused (introduction, a dialog): the purchase countdown stands at the full timeout
            SecondsLeft = session.Paused
                ? (int)sessions.Timeout.TotalSeconds
                : Math.Max(0, (int)Math.Ceiling((sessions.ExpiresAt(session) - DateTime.UtcNow).TotalSeconds))
        };
    }
}
