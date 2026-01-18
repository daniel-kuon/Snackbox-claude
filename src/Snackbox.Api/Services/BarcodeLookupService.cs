using System.Net;
using System.Text.Json;
using Snackbox.Api.Dtos;

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
        
        var apiKey = configuration["SearchUpcData:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.Equals(apiKey, "YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SearchUpcData API key is not configured or is invalid. Please set a valid API key.");
        }
        _apiKey = apiKey;
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
