using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;

namespace Snackbox.Api.Services;

/// <summary>
/// What a user owes (spent minus paid; negative = credit) - the one rule the kiosk, the tablet,
/// the admin views and the payment reminders all use. A withdrawal counts as owed: an admin who
/// took money from the cash register or received a PayPal payment for the box holds the box's money.
/// </summary>
public static class UserBalance
{
    public static async Task<decimal> OpenAmountAsync(ApplicationDbContext context, int userId) =>
        (await OpenAmountsAsync(context, userId)).GetValueOrDefault(userId);

    /// <summary>Every user's open amount (or one user's); users without any booking are missing.</summary>
    public static async Task<Dictionary<int, decimal>> OpenAmountsAsync(ApplicationDbContext context, int? userId = null)
    {
        // A purchase counts as ManualAmount when set, otherwise as the sum of its scans. Manual
        // purchases (admin entries, corrections and imported legacy purchases without a mappable
        // barcode) have no scans, so summing scans alone would silently under-count what is owed.
        var fromScans = await context.BarcodeScans
            .Where(bs => bs.Purchase.ManualAmount == null && (userId == null || bs.Purchase.UserId == userId))
            .GroupBy(bs => bs.Purchase.UserId)
            .Select(g => new { UserId = g.Key, Sum = g.Sum(bs => bs.Amount) })
            .ToListAsync();

        var manual = await context.Purchases
            .Where(p => p.ManualAmount != null && (userId == null || p.UserId == userId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Sum = g.Sum(p => p.ManualAmount!.Value) })
            .ToListAsync();

        var discounts = await context.PurchaseDiscounts
            .Where(pd => userId == null || pd.Purchase.UserId == userId)
            .GroupBy(pd => pd.Purchase.UserId)
            .Select(g => new { UserId = g.Key, Sum = g.Sum(pd => pd.DiscountAmount) })
            .ToListAsync();

        var paid = await context.Payments
            .Where(p => userId == null || p.UserId == userId)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Sum = g.Sum(p => p.Amount) })
            .ToListAsync();

        var withdrawn = await context.Withdrawals
            .Where(w => userId == null || w.UserId == userId)
            .GroupBy(w => w.UserId)
            .Select(g => new { UserId = g.Key, Sum = g.Sum(w => w.Amount) })
            .ToListAsync();

        var open = new Dictionary<int, decimal>();
        void Add(int id, decimal amount) => open[id] = open.GetValueOrDefault(id) + amount;
        foreach (var x in fromScans) Add(x.UserId, x.Sum);
        foreach (var x in manual) Add(x.UserId, x.Sum);
        foreach (var x in discounts) Add(x.UserId, -x.Sum);
        foreach (var x in paid) Add(x.UserId, -x.Sum);
        foreach (var x in withdrawn) Add(x.UserId, x.Sum);
        return open;
    }
}
