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

// DTO for API response from searchupcdata.com
public class SearchUpcDataApiResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("upc")]
    public string? Upc { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
}
