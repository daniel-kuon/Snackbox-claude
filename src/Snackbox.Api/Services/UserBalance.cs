using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;

namespace Snackbox.Api.Services;

/// <summary>What a user owes as the kiosk shows it (spent minus paid; negative = credit).</summary>
public static class UserBalance
{
    public static async Task<decimal> OpenAmountAsync(ApplicationDbContext context, int userId)
    {
        // A purchase counts as ManualAmount when set, otherwise as the sum of its scans -
        // the same rule the admin views use. Manual purchases (admin entries, corrections and
        // imported legacy purchases without a mappable barcode) have no scans, so summing
        // scans alone would silently under-count what the user owes.
        var totalFromScans = await context.BarcodeScans
            .Where(bs => bs.Purchase.UserId == userId && bs.Purchase.ManualAmount == null)
            .SumAsync(bs => bs.Amount);

        var totalManual = await context.Purchases
            .Where(p => p.UserId == userId && p.ManualAmount != null)
            .SumAsync(p => p.ManualAmount!.Value);

        var totalDiscounts = await context.PurchaseDiscounts
            .Where(pd => pd.Purchase.UserId == userId)
            .SumAsync(pd => pd.DiscountAmount);

        var totalPaid = await context.Payments
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.Amount);

        return totalFromScans + totalManual - totalDiscounts - totalPaid;
    }
}
