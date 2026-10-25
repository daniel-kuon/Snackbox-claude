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
    IProductBestBeforeDateService bestBeforeDates) : ControllerBase
{
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

        var product = await context.Products.FindAsync(productId);
        if (product?.Price == null) return NotFound(new { message = "Product not available" });

        var now = DateTime.UtcNow;
        var scan = new BarcodeScan
        {
            PurchaseId = session.PurchaseId,
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

        await achievementService.CheckImmediateAchievementsAsync(session.UserId, session.PurchaseId);

        return Ok(await BuildStateAsync(sessions.Current ?? session));
    }

    /// <summary>One item less, or the product's line removed completely with <paramref name="all"/>.</summary>
    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<TabletStateDto>> RemoveItem(int productId, [FromQuery] bool all = false)
    {
        var session = sessions.Current;
        if (session == null) return Conflict(new { message = "Scan your card first" });

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

    /// <summary>Done: the purchase stays booked, the tablet goes back to showing the shelf.</summary>
    [HttpPost("complete")]
    public ActionResult Complete()
    {
        sessions.End();
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
        var purchase = await context.Purchases.FindAsync(session.PurchaseId);
        if (purchase != null) purchase.UpdatedAt = now;
        await context.SaveChangesAsync();

        var total = await context.BarcodeScans.Where(s => s.PurchaseId == session.PurchaseId).SumAsync(s => s.Amount);
        await PurchaseDiscountCalculator.ApplyAsync(context, session.PurchaseId, total);
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
            Username = session.Username,
            Items = scans
                .Where(s => s.ProductId != null)
                .GroupBy(s => new { s.ProductId, s.ProductName, s.Amount })
                .Select(g => new TabletItemDto { ProductId = g.Key.ProductId!.Value, Name = g.Key.ProductName ?? "?", UnitPrice = g.Key.Amount, Quantity = g.Count() })
                .ToList(),
            Total = total,
            DiscountedTotal = Math.Max(0, total - discounts),
            SecondsLeft = Math.Max(0, (int)(session.LastActivity + sessions.Timeout - DateTime.UtcNow).TotalSeconds)
        };
    }
}
