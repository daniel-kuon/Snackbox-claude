using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public interface IProductCatalogService
{
    /// <summary>
    /// Finds the product behind a scanned product barcode. An unknown code is looked up online
    /// and assigned to the existing product whose name matches, or becomes a new product
    /// (with the picture from the lookup) - so a bulk scan never has to stop for it.
    /// </summary>
    Task<BarcodeResolveResultDto> ResolveAsync(BarcodeResolveRequest request);

    /// <summary>Downloads a picture and stores it as the product's image. False if it could not be fetched.</summary>
    Task<bool> DownloadImageAsync(int productId, string url);
}

[Traced(LogReturnValue = true)] // which product an unknown code ended up on, and why
public partial class ProductCatalogService(
    ApplicationDbContext context,
    IBarcodeLookupService lookup,
    IHttpClientFactory httpClientFactory,
    IStockCalculationService stockCalculation,
    ILogger<ProductCatalogService> logger) : IProductCatalogService
{
    private const int MaxImageBytes = 2 * 1024 * 1024;

    public async Task<BarcodeResolveResultDto> ResolveAsync(BarcodeResolveRequest request)
    {
        var barcode = request.Barcode.Trim();

        var known = await context.ProductBarcodes.AsNoTracking().FirstOrDefaultAsync(pb => pb.Barcode == barcode);
        if (known != null)
        {
            return new BarcodeResolveResultDto
            {
                Status = BarcodeResolveStatus.Known,
                Product = await LoadDtoAsync(known.ProductId),
                BarcodeQuantity = known.Quantity
            };
        }

        var online = await lookup.LookupBarcodeAsync(barcode);
        var found = online.Success ? online.Product : null;

        if (found == null && string.IsNullOrWhiteSpace(request.Name))
        {
            return new BarcodeResolveResultDto { Status = BarcodeResolveStatus.NotFound };
        }

        Product? product = null;
        if (found != null && !request.ForceNewProduct)
        {
            var candidates = await context.Products.AsNoTracking().Select(p => new { p.Id, p.Name }).ToListAsync();
            var matchId = FindMatch(found.Title, found.Brand, candidates.Select(c => (c.Id, c.Name)));
            if (matchId != null) product = await context.Products.FindAsync(matchId.Value);
        }

        var status = product != null ? BarcodeResolveStatus.AssignedToExisting : BarcodeResolveStatus.Created;
        if (product == null)
        {
            product = new Product
            {
                Name = !string.IsNullOrWhiteSpace(request.Name) ? request.Name.Trim() : ProductName(found!),
                CreatedAt = DateTime.UtcNow
            };
            context.Products.Add(product);
        }

        context.ProductBarcodes.Add(new ProductBarcode { Product = product, Barcode = barcode, Quantity = 1, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        logger.LogInformation("Unknown barcode {Barcode} {Status} product {ProductId} ({ProductName}), online title {OnlineTitle}",
                              barcode, status, product.Id, product.Name, found?.Title);

        // A picture makes the product show up properly on the tablet; an existing product keeps its own
        if (!string.IsNullOrWhiteSpace(found?.ImageUrl) && !await context.ProductImages.AnyAsync(i => i.ProductId == product.Id))
        {
            await DownloadImageAsync(product.Id, found.ImageUrl);
        }

        return new BarcodeResolveResultDto
        {
            Status = status,
            Product = await LoadDtoAsync(product.Id),
            OnlineTitle = found?.Title
        };
    }

    public async Task<bool> DownloadImageAsync(int productId, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return false;

        try
        {
            using var http = httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Snackbox/1.0 (+https://github.com/daniel-kuon/Snackbox-claude)");
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (!response.IsSuccessStatusCode || contentType?.StartsWith("image/") != true) return false;
            if (response.Content.Headers.ContentLength > MaxImageBytes) return false;

            var data = await response.Content.ReadAsByteArrayAsync();
            if (data.Length > MaxImageBytes) return false;

            await SaveImageAsync(productId, data, contentType);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Could not download the picture for product {ProductId} from {Url}", productId, url);
            return false;
        }
    }

    private async Task SaveImageAsync(int productId, byte[] data, string contentType)
    {
        var image = await context.ProductImages.FindAsync(productId);
        if (image == null)
        {
            context.ProductImages.Add(new ProductImage { ProductId = productId, Data = data, ContentType = contentType, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            image.Data = data;
            image.ContentType = contentType;
            image.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
    }

    private async Task<ProductDto> LoadDtoAsync(int productId)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Barcodes)
            .Include(p => p.Batches).ThenInclude(b => b.ShelvingActions)
            .FirstAsync(p => p.Id == productId);
        var dto = Mappers.ProductMapper.ToDto(product, stockCalculation);
        dto.HasImage = await context.ProductImages.AnyAsync(i => i.ProductId == productId);
        return dto;
    }

    private static string ProductName(BarcodeLookupProductDto found) =>
        string.IsNullOrWhiteSpace(found.Brand) || found.Title.Contains(found.Brand, StringComparison.OrdinalIgnoreCase)
            ? found.Title
            : $"{found.Brand} {found.Title}";

    /// <summary>
    /// The existing product whose name is fully contained, word by word, in the online title and
    /// brand - "Mars" matches "Mars Riegel 51g" but not "Marshmallows". The most specific name
    /// wins ("Ritter Sport Marzipan" over "Ritter Sport"); a tie is ambiguous and matches nothing,
    /// so a new product is created rather than stock booked onto the wrong one.
    /// </summary>
    public static int? FindMatch(string title, string? brand, IEnumerable<(int Id, string Name)> products)
    {
        var online = Words($"{title} {brand}");
        var matches = products
            .Select(p => (p.Id, Words: Words(p.Name)))
            .Where(p => p.Words.Count > 0 && p.Words.IsSubsetOf(online))
            .OrderByDescending(p => p.Words.Count)
            .ToList();

        if (matches.Count == 0) return null;
        if (matches.Count > 1 && matches[1].Words.Count == matches[0].Words.Count) return null;
        return matches[0].Id;
    }

    // Words of a name without sizes and weights ("51g", "6x", "0,5l") - those differ between packs
    private static HashSet<string> Words(string name) =>
        WordRegex().Matches(name.ToLowerInvariant())
                   .Select(m => m.Value)
                   .Where(w => w.Length > 1 && !w.Any(char.IsDigit))
                   .ToHashSet();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordRegex();
}
