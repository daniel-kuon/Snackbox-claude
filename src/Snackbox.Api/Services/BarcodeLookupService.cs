using System.Net;
using System.Text.Json;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

[Traced(LogReturnValue = true)] // external UPC lookups: what was asked and what came back
public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BarcodeLookupService> _logger;
    private readonly string? _apiKey;

    public BarcodeLookupService(HttpClient httpClient, IConfiguration configuration, ILogger<BarcodeLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // searchupcdata.com needs a paid key and is only asked when Open Food Facts does not
        // know the code. Without a key the lookup used to throw on construction, which broke
        // every page that looks up a barcode on an installation.
        var apiKey = configuration["SearchUpcData:ApiKey"];
        _apiKey = string.IsNullOrWhiteSpace(apiKey) || string.Equals(apiKey, "YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase)
            ? null
            : apiKey;
    }

    public async Task<BarcodeLookupResponseDto> LookupBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return new BarcodeLookupResponseDto
            {
                Success = false,
                ErrorMessage = "Barcode cannot be empty"
            };
        }

        var openFoodFacts = await LookupOpenFoodFactsAsync(barcode);
        if (openFoodFacts.Success || _apiKey == null) return openFoodFacts;

        return await LookupSearchUpcDataAsync(barcode);
    }

    /// <summary>Open Food Facts: free, no key, good coverage of German snacks, and has pictures.</summary>
    private async Task<BarcodeLookupResponseDto> LookupOpenFoodFactsAsync(string barcode)
    {
        try
        {
            var url = $"https://world.openfoodfacts.org/api/v2/product/{Uri.EscapeDataString(barcode)}.json" +
                      "?fields=product_name,product_name_de,brands,categories,quantity,image_front_url";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Open Food Facts asks every client to identify itself
            request.Headers.UserAgent.ParseAdd("Snackbox/1.0 (+https://github.com/daniel-kuon/Snackbox-claude)");

            using var response = await _httpClient.SendAsync(request);
            var body = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<OpenFoodFactsResponse>() : null;
            var product = body?.Status == 1 ? body.Product : null;
            var name = product == null ? null : FirstNonEmpty(product.ProductNameDe, product.ProductName);

            if (product == null || name == null)
            {
                return new BarcodeLookupResponseDto { Success = false, ErrorMessage = "Product not found for this barcode." };
            }

            return new BarcodeLookupResponseDto
            {
                Success = true,
                Product = new BarcodeLookupProductDto
                {
                    Title = string.IsNullOrWhiteSpace(product.Quantity) || Squeezed(name).Contains(Squeezed(product.Quantity))
                        ? name
                        : $"{name} {product.Quantity.Trim()}",
                    // Often "Mars Wrigley Confectionery UK Limited, Snickers": the shortest is the brand
                    // people know, the long ones are the manufacturer
                    Brand = product.Brands?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                          .OrderBy(b => b.Length).FirstOrDefault(),
                    Category = product.Categories?.Split(',')[0].Trim(),
                    ImageUrl = product.ImageFrontUrl,
                    Barcode = barcode
                }
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Open Food Facts lookup failed for {Barcode}", barcode);
            return new BarcodeLookupResponseDto { Success = false, ErrorMessage = "Network error occurred while looking up barcode" };
        }
    }

    // "Bounty Trio 85g" already says "85 g"
    private static string Squeezed(string text) => text.Replace(" ", "").ToLowerInvariant();

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private async Task<BarcodeLookupResponseDto> LookupSearchUpcDataAsync(string barcode)
    {
        try
        {
            var url = $"https://searchupcdata.com/api/products/{Uri.EscapeDataString(barcode)}";
            
            _logger.LogInformation("Looking up barcode: {Barcode}", barcode);
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Barcode lookup failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                
                var errorMessage = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Invalid API key. Please check your configuration.",
                    HttpStatusCode.Forbidden => "Your API key has been deactivated. Contact support for assistance.",
                    HttpStatusCode.NotFound => "Product not found for this barcode.",
                    (HttpStatusCode)429 => "Monthly quota exceeded. Upgrade your plan or wait for next month's reset.",
                    HttpStatusCode.InternalServerError => "An unexpected error occurred. Please try again later.",
                    _ => $"API request failed with status {response.StatusCode}"
                };
                
                return new BarcodeLookupResponseDto
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<SearchUpcDataApiResponse>();
            
            if (apiResponse == null)
            {
                _logger.LogInformation("No product found for barcode: {Barcode}", barcode);
                
                return new BarcodeLookupResponseDto
                {
                    Success = false,
                    ErrorMessage = "No product found for this barcode"
                };
            }
            
            return new BarcodeLookupResponseDto
            {
                Success = true,
                Product = new BarcodeLookupProductDto
                {
                    Title = apiResponse.Name ?? "Unknown Product",
                    Manufacturer = null, // searchupcdata.com doesn't provide manufacturer field
                    Brand = apiResponse.Brand,
                    Description = apiResponse.Description,
                    Category = apiResponse.Category,
                    ImageUrl = apiResponse.ImageUrl,
                    Barcode = apiResponse.Upc ?? barcode
                }
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse API response for barcode: {Barcode}", barcode);
            return new BarcodeLookupResponseDto
            {
                Success = false,
                ErrorMessage = "Failed to parse API response. The service may be experiencing issues."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while looking up barcode: {Barcode}", barcode);
            return new BarcodeLookupResponseDto
            {
                Success = false,
                ErrorMessage = "Network error occurred while looking up barcode"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while looking up barcode: {Barcode}", barcode);
            return new BarcodeLookupResponseDto
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred"
            };
        }
    }
}
