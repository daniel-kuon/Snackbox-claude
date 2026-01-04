namespace Snackbox.Api.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime? BestBeforeInStock { get; set; }
    public required DateTime? BestBeforeOnShelf { get; set; }
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
}

public class UpdateProductDto
{
    public required string Name { get; set; }
    public required string Barcode { get; set; }
    public decimal Price { get; set; }
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
