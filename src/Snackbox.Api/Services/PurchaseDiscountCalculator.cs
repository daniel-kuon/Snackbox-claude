using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

/// <summary>Re-applies the currently valid discounts to a purchase after it changed.</summary>
public static class PurchaseDiscountCalculator
{
    public static async Task<(List<AppliedDiscountDto> Applied, decimal DiscountedAmount)> ApplyAsync(
        ApplicationDbContext context, int purchaseId, decimal totalAmount)
    {
        var now = DateTime.UtcNow;
        var applicableDiscounts = await context.Discounts
            .Where(d => d.IsActive
                && d.ValidFrom <= now
                && d.ValidTo >= now
                && d.MinimumPurchaseAmount <= totalAmount)
            .ToListAsync();

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
                : totalAmount * (discount.Value / 100);

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
}
