namespace Snackbox.Api.DTOs;

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

// Internal DTO for API response from barcodelookup.com
internal class BarcodeLookupApiResponse
{
    public List<BarcodeLookupApiProduct>? Products { get; set; }
}

internal class BarcodeLookupApiProduct
{
    public string? Barcode_Number { get; set; }
    public string? Title { get; set; }
    public string? Manufacturer { get; set; }
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
}
