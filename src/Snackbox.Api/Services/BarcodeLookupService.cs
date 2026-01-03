using System.Net.Http.Json;
using Snackbox.Api.DTOs;

namespace Snackbox.Api.Services;

public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BarcodeLookupService> _logger;
    private readonly string _apiKey;

    public BarcodeLookupService(HttpClient httpClient, IConfiguration configuration, ILogger<BarcodeLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["BarcodeLookup:ApiKey"] ?? throw new InvalidOperationException("BarcodeLookup API key is not configured");
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

        try
        {
            var url = $"https://api.barcodelookup.com/v3/products?barcode={Uri.EscapeDataString(barcode)}&key={Uri.EscapeDataString(_apiKey)}";
            
            _logger.LogInformation("Looking up barcode: {Barcode}", barcode);
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Barcode lookup failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                
                return new BarcodeLookupResponseDto
                {
                    Success = false,
                    ErrorMessage = $"API request failed with status {response.StatusCode}"
                };
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<BarcodeLookupApiResponse>();
            
            if (apiResponse?.Products == null || !apiResponse.Products.Any())
            {
                _logger.LogInformation("No products found for barcode: {Barcode}", barcode);
                
                return new BarcodeLookupResponseDto
                {
                    Success = false,
                    ErrorMessage = "No product found for this barcode"
                };
            }

            var product = apiResponse.Products.First();
            
            return new BarcodeLookupResponseDto
            {
                Success = true,
                Product = new BarcodeLookupProductDto
                {
                    Title = product.Title ?? "Unknown Product",
                    Manufacturer = product.Manufacturer,
                    Brand = product.Brand,
                    Description = product.Description,
                    Category = product.Category,
                    Barcode = barcode
                }
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
