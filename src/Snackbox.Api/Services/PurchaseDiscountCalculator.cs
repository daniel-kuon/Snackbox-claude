using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

/// <summary>Re-applies the currently valid discounts to a purchase after it changed.</summary>
public static class PurchaseDiscountCalculator
{
    public static async Task<(List<AppliedDiscountDto> Applied, decimal DiscountedAmount)> ApplyAsync(
        ApplicationDbContext context, int purchaseId, decimal totalAmount, TimeProvider? time = null)
    {
        time ??= TimeProvider.System;
        var now = time.GetUtcNow().UtcDateTime;
        var applicableDiscounts = await context.Discounts
            .Where(d => d.IsActive
                && d.ValidFrom <= now
                && d.ValidTo >= now
                && d.MinimumPurchaseAmount <= totalAmount)
            .ToListAsync();

        if (applicableDiscounts.Any(d => d.ThemeKey != null || d.FirstPurchaseOnly))
        {
            applicableDiscounts = await OnlyThemedAndFirstAsync(context, purchaseId, applicableDiscounts, time);
        }

        // Remove any existing discounts for this purchase (in case items were removed)
        var existingDiscounts = await context.PurchaseDiscounts
            .Where(pd => pd.PurchaseId == purchaseId)
            .ToListAsync();
        context.PurchaseDiscounts.RemoveRange(existingDiscounts);

        var appliedDiscounts = new List<AppliedDiscountDto>();
        var totalDiscountAmount = 0m;

        foreach (var discount in applicableDiscounts)
        {
            var discountAmount = discount.Type == DiscountType.FixedAmount
                ? Math.Min(discount.Value, totalAmount)
                : Math.Round(totalAmount * (discount.Value / 100), 2);

            totalDiscountAmount += discountAmount;

            context.PurchaseDiscounts.Add(new PurchaseDiscount
            {
                PurchaseId = purchaseId,
                DiscountId = discount.Id,
                DiscountAmount = discountAmount
            });

            appliedDiscounts.Add(new AppliedDiscountDto
            {
                DiscountId = discount.Id,
                Name = discount.Name,
                Type = discount.Type.ToString(),
                Value = discount.Value,
                DiscountAmount = discountAmount
            });
        }

        await context.SaveChangesAsync();

        // Apply all discounts (but don't go below zero)
        return (appliedDiscounts, Math.Max(0, totalAmount - totalDiscountAmount));
    }

    /// <summary>
    /// Drops theme discounts whose theme is off and first-purchase discounts on a user's later
    /// purchases of the day. Both go by the kiosk PC's local day, like the decoration.
    /// </summary>
    private static async Task<List<Discount>> OnlyThemedAndFirstAsync(
        ApplicationDbContext context, int purchaseId, List<Discount> discounts, TimeProvider time)
    {
        var localNow = time.GetLocalNow().DateTime;
        var themes = await context.Themes.ToDictionaryAsync(t => t.Key);
        var purchase = await context.Purchases.Where(p => p.Id == purchaseId)
                                              .Select(p => new { p.UserId, p.CreatedAt })
                                              .FirstAsync();

        var today = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified), time.LocalTimeZone);
        var earlierToday = await context.Purchases.AnyAsync(p => p.UserId == purchase.UserId
                                                                 && p.Id != purchaseId
                                                                 && p.CreatedAt >= today
                                                                 && p.CreatedAt < purchase.CreatedAt);

        return discounts.Where(d => (d.ThemeKey == null || (themes.TryGetValue(d.ThemeKey, out var theme) && theme.IsActiveOn(localNow)))
                                    && !(d.FirstPurchaseOnly && earlierToday))
                        .ToList();
    }
}
