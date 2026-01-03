using System.Text.Json.Serialization;

namespace Snackbox.Api.Dtos;

public class BarcodeLookupResponseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public BarcodeLookupProductDto? Product { get; set; }
}

public class BarcodeLookupProductDto
{
    public required string Title { get; set; }
    public string? Manufacturer { get; set; }
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Barcode { get; set; } = string.Empty;
}

// DTO for API response from barcodelookup.com
public class BarcodeLookupApiResponse
{
    public List<BarcodeLookupApiProduct>? Products { get; set; }
}

public class BarcodeLookupApiProduct
{
    [JsonPropertyName("barcode_number")]
    public string? BarcodeNumber { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}
