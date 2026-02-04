using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Models;

namespace Snackbox.Api.Data;

/// <summary>
/// Extension methods for common EF Core query patterns
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Includes all financial data for a user (Payments, Purchases with Scans, Withdrawals)
    /// </summary>
    public static IQueryable<User> IncludeFinancialData(this IQueryable<User> query)
    {
        return query
            .Include(u => u.Purchases)
                .ThenInclude(p => p.Scans)
            .Include(u => u.Payments)
            .Include(u => u.Withdrawals);
    }

    /// <summary>
    /// Includes all related data for a product (Barcodes and Batches)
    /// </summary>
    public static IQueryable<Product> IncludeRelatedData(this IQueryable<Product> query)
    {
        return query
            .Include(p => p.Barcodes)
            .Include(p => p.Batches);
    }

    /// <summary>
    /// Includes batches for a product
    /// </summary>
    public static IQueryable<Product> IncludeBatches(this IQueryable<Product> query)
    {
        return query.Include(p => p.Batches);
    }

    /// <summary>
    /// Includes batches with shelving actions for a product
    /// </summary>
    public static IQueryable<Product> IncludeBatchesWithShelvingActions(this IQueryable<Product> query)
    {
        return query
            .Include(p => p.Batches)
                .ThenInclude(b => b.ShelvingActions);
    }

    /// <summary>
    /// Includes barcodes for a product
    /// </summary>
    public static IQueryable<Product> IncludeBarcodes(this IQueryable<Product> query)
    {
        return query.Include(p => p.Barcodes);
    }

    /// <summary>
    /// Includes all related data for a product (Barcodes and Batches with ShelvingActions)
    /// </summary>
    public static IQueryable<Product> IncludeRelatedDataWithShelvingActions(this IQueryable<Product> query)
    {
        return query
            .Include(p => p.Barcodes)
            .Include(p => p.Batches)
                .ThenInclude(b => b.ShelvingActions);
    }
}
