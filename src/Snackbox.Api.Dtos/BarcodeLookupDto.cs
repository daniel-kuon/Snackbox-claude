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
    public string? ImageUrl { get; set; }
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

// Open Food Facts product API (https://world.openfoodfacts.org/api/v2/product/{code}.json)
public class OpenFoodFactsResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; set; }
}

public class OpenFoodFactsProduct
{
    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_name_de")]
    public string? ProductNameDe { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("categories")]
    public string? Categories { get; set; }

    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }
}
