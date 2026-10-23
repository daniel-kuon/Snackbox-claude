namespace Snackbox.Api.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime? BestBeforeInStock { get; set; }
    public required DateTime? BestBeforeOnShelf { get; set; }
    public double AverageProductsShelvedPerWeek { get; set; }
    public decimal? Price { get; set; }
    public bool HasImage { get; set; }
    public List<ProductBarcodeDto> Barcodes { get; set; } = new();
}

public class ProductBarcodeDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string Barcode { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductDto
{
    public required string Name { get; set; }
    public decimal? Price { get; set; }
}

public class UpdateProductDto
{
    public required string Name { get; set; }
    public decimal? Price { get; set; }
}

public class CreateProductBarcodeDto
{
    public required string Barcode { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateProductBarcodeDto
{
    public required string Barcode { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>What happened to a scanned product barcode in the bulk shelving / inventory scan.</summary>
public enum BarcodeResolveStatus
{
    Known,              // The barcode already belonged to a product
    AssignedToExisting, // Unknown, found online and matched to an existing product by name
    Created,            // Unknown, found online, no match: a new product was created
    NotFound            // Unknown here and online - the admin has to name it
}

public class BarcodeResolveRequest
{
    public required string Barcode { get; set; }

    /// <summary>Name for a new product when the code is unknown here and online.</summary>
    public string? Name { get; set; }

    /// <summary>Do not match an existing product - always create a new one.</summary>
    public bool ForceNewProduct { get; set; }
}

public class BarcodeResolveResultDto
{
    public BarcodeResolveStatus Status { get; set; }
    public ProductDto? Product { get; set; }
    public int BarcodeQuantity { get; set; } = 1;
    public string? OnlineTitle { get; set; }
}

public class InventoryCountRequest
{
    public int ProductId { get; set; }
    public int ShelfQuantity { get; set; }
}

public class ImageSuggestionDto
{
    public required string ImageUrl { get; set; }
    public required string ThumbnailUrl { get; set; }
    public string? Title { get; set; }

    /// <summary>"Open Food Facts" or "Google Images"</summary>
    public required string Source { get; set; }
}

public class ImageSuggestionsDto
{
    public List<ImageSuggestionDto> Suggestions { get; set; } = new();

    /// <summary>False while no SerpApi key is configured - the page then offers a Google Images link.</summary>
    public bool GoogleImagesEnabled { get; set; }
}

public class ImageFromUrlRequest
{
    public required string Url { get; set; }
}
