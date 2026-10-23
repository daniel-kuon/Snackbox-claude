using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public interface IProductImageSearch
{
    /// <summary>Pictures to choose from for a product, best guesses first.</summary>
    Task<ImageSuggestionsDto> SuggestAsync(int productId);
}

/// <summary>
/// Picture suggestions: Open Food Facts by the product's barcodes and by its name (free), and
/// Google Images through serpapi.com when <c>ImageSearch:SerpApiKey</c> is set. Google's own
/// image search API (Custom Search JSON API) takes no new customers and ends on 2027-01-01, and
/// scraping Google directly breaks their terms - SerpApi is the supported way to Google results.
/// </summary>
[Traced(LogReturnValue = true)]
public class ProductImageSearch(
    ApplicationDbContext context,
    IBarcodeLookupService lookup,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ProductImageSearch> logger) : IProductImageSearch
{
    private const int MaxPerSource = 8;
    private const string UserAgent = "Snackbox/1.0 (+https://github.com/daniel-kuon/Snackbox-claude)";

    public async Task<ImageSuggestionsDto> SuggestAsync(int productId)
    {
        var product = await context.Products.AsNoTracking().Include(p => p.Barcodes).FirstOrDefaultAsync(p => p.Id == productId);
        var result = new ImageSuggestionsDto { GoogleImagesEnabled = !string.IsNullOrWhiteSpace(SerpApiKey) };
        if (product == null) return result;

        var bySource = await Task.WhenAll(
            ByBarcodesAsync(product.Barcodes.Select(b => b.Barcode)),
            OpenFoodFactsByNameAsync(product.Name),
            GoogleImagesAsync(product.Name));

        result.Suggestions = bySource.SelectMany(s => s).DistinctBy(s => s.ImageUrl).ToList();
        return result;
    }

    private string? SerpApiKey => configuration["ImageSearch:SerpApiKey"];

    // The exact product: the barcode lookup's front picture
    private async Task<List<ImageSuggestionDto>> ByBarcodesAsync(IEnumerable<string> barcodes)
    {
        var found = new List<ImageSuggestionDto>();
        foreach (var barcode in barcodes.Take(3))
        {
            var online = await lookup.LookupBarcodeAsync(barcode);
            if (online.Product?.ImageUrl is { } url)
            {
                found.Add(new ImageSuggestionDto { ImageUrl = url, ThumbnailUrl = url, Title = online.Product.Title, Source = "Open Food Facts" });
            }
        }
        return found;
    }

    private async Task<List<ImageSuggestionDto>> OpenFoodFactsByNameAsync(string name)
    {
        var url = $"https://search.openfoodfacts.org/search?q={Uri.EscapeDataString(name)}&page_size=20&fields=product_name,brands,image_front_url,image_front_small_url";
        var response = await GetJsonAsync<OpenFoodFactsSearchResponse>(url);

        return (response?.Hits ?? [])
            .Where(h => !string.IsNullOrWhiteSpace(h.ImageFrontUrl))
            .Take(MaxPerSource)
            .Select(h => new ImageSuggestionDto
            {
                ImageUrl = h.ImageFrontUrl!,
                ThumbnailUrl = h.ImageFrontSmallUrl ?? h.ImageFrontUrl!,
                Title = h.ProductName,
                Source = "Open Food Facts"
            })
            .ToList();
    }

    private async Task<List<ImageSuggestionDto>> GoogleImagesAsync(string name)
    {
        var key = SerpApiKey;
        if (string.IsNullOrWhiteSpace(key)) return [];

        var url = $"https://serpapi.com/search.json?engine=google_images&q={Uri.EscapeDataString(name)}&gl=de&hl=de&api_key={Uri.EscapeDataString(key)}";
        var response = await GetJsonAsync<SerpApiImagesResponse>(url, logUrl: "serpapi.com google_images");

        return (response?.ImagesResults ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r.Original) && r.Original.StartsWith("https://"))
            .Take(MaxPerSource * 2)
            .Select(r => new ImageSuggestionDto
            {
                ImageUrl = r.Original!,
                ThumbnailUrl = r.Thumbnail ?? r.Original!,
                Title = r.Title,
                Source = "Google Images"
            })
            .ToList();
    }

    // A source that does not answer just contributes nothing
    private async Task<T?> GetJsonAsync<T>(string url, string? logUrl = null) where T : class
    {
        try
        {
            using var http = httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            using var response = await http.SendAsync(request);
            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<T>();

            // logUrl keeps the SerpApi key out of the log
            logger.LogWarning("Picture search {Url} answered {Status}", logUrl ?? url, response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Picture search {Url} failed", logUrl ?? url);
        }
        return null;
    }

    private class OpenFoodFactsSearchResponse
    {
        [JsonPropertyName("hits")] public List<Hit>? Hits { get; set; }

        public class Hit
        {
            [JsonPropertyName("product_name")] public string? ProductName { get; set; }
            [JsonPropertyName("image_front_url")] public string? ImageFrontUrl { get; set; }
            [JsonPropertyName("image_front_small_url")] public string? ImageFrontSmallUrl { get; set; }
        }
    }

    private class SerpApiImagesResponse
    {
        [JsonPropertyName("images_results")] public List<Result>? ImagesResults { get; set; }

        public class Result
        {
            [JsonPropertyName("original")] public string? Original { get; set; }
            [JsonPropertyName("thumbnail")] public string? Thumbnail { get; set; }
            [JsonPropertyName("title")] public string? Title { get; set; }
        }
    }
}
