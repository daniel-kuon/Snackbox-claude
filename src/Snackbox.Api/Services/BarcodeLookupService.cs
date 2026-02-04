using System.Net;
using System.Text.Json;
using Refit;
using Snackbox.Api.Dtos;
using Snackbox.Api.External;

namespace Snackbox.Api.Services;

public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly IExternalBarcodeApi _externalApi;
    private readonly ILogger<BarcodeLookupService> _logger;

    public BarcodeLookupService(IExternalBarcodeApi externalApi, ILogger<BarcodeLookupService> logger)
    {
        _externalApi = externalApi;
        _logger = logger;
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
            _logger.LogInformation("Looking up barcode: {Barcode}", barcode);

            var apiResponse = await _externalApi.GetProductAsync(barcode);

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
        catch (ApiException apiEx)
        {
            var statusCode = (HttpStatusCode)apiEx.StatusCode;
            _logger.LogWarning(apiEx, "Barcode lookup failed with status {StatusCode}", statusCode);

            var errorMessage = statusCode switch
            {
                HttpStatusCode.Unauthorized => "Invalid API key. Please check your configuration.",
                HttpStatusCode.Forbidden => "Your API key has been deactivated. Contact support for assistance.",
                HttpStatusCode.NotFound => "Product not found for this barcode.",
                (HttpStatusCode)429 => "Monthly quota exceeded. Upgrade your plan or wait for next month's reset.",
                HttpStatusCode.InternalServerError => "An unexpected error occurred. Please try again later.",
                _ => $"API request failed with status {statusCode}"
            };

            return new BarcodeLookupResponseDto
            {
                Success = false,
                ErrorMessage = errorMessage
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
