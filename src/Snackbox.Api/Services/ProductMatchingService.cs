using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using System.Text.RegularExpressions;

namespace Snackbox.Api.Services;

public interface IProductMatchingService
{
    Task<ProductMatchResult?> FindMatchingProduct(string barcode, string productName);
}

public partial class ProductMatchingService : IProductMatchingService
{
    private readonly ApplicationDbContext _context;

    // Compiled regex patterns for performance
    [GeneratedRegex(@"MHD:\d{1,2}\.\d{1,2}\.\d{2,4}")]
    private static partial Regex MhdDateRegex();
    
    [GeneratedRegex(@"\d+[gkmlt]+", RegexOptions.IgnoreCase)]
    private static partial Regex WeightSizeRegex();
    
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public ProductMatchingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductMatchResult?> FindMatchingProduct(string barcode, string productName)
    {
        // First try to find by exact barcode match
        var productByBarcode = await _context.ProductBarcodes
            .Include(pb => pb.Product)
            .FirstOrDefaultAsync(pb => pb.Barcode == barcode);

        if (productByBarcode != null)
        {
            return new ProductMatchResult
            {
                ProductId = productByBarcode.ProductId,
                ProductName = productByBarcode.Product.Name,
                MatchType = "ExactBarcode",
                Confidence = 1.0m
            };
        }

        // Try fuzzy name matching (simple contains check for now)
        var cleanedSearchName = CleanProductName(productName);
        var products = await _context.Products
            .Include(p => p.Barcodes)
            .ToListAsync();

        foreach (var product in products)
        {
            var cleanedProductName = CleanProductName(product.Name);
            
            // Check if names match or contain each other
            if (cleanedProductName.Equals(cleanedSearchName, StringComparison.OrdinalIgnoreCase))
            {
                return new ProductMatchResult
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    MatchType = "ExactName",
                    Confidence = 0.9m
                };
            }
            
            if (cleanedProductName.Contains(cleanedSearchName, StringComparison.OrdinalIgnoreCase) ||
                cleanedSearchName.Contains(cleanedProductName, StringComparison.OrdinalIgnoreCase))
            {
                return new ProductMatchResult
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    MatchType = "PartialName",
                    Confidence = 0.7m
                };
            }
        }

        return null;
    }

    private string CleanProductName(string name)
    {
        // Remove common suffixes, weights, dates, etc.
        var cleaned = name;
        
        // Remove MHD dates
        cleaned = MhdDateRegex().Replace(cleaned, "");
        
        // Remove weights/sizes
        cleaned = WeightSizeRegex().Replace(cleaned, "");
        
        // Remove extra whitespace
        cleaned = WhitespaceRegex().Replace(cleaned, " ").Trim();
        
        return cleaned;
    }
}

public class ProductMatchResult
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string MatchType { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
}
