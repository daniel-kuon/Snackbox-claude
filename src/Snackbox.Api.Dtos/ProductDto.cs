namespace Snackbox.Api.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Barcode { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime? BestBeforeInStock { get; set; }
    public required DateTime? BestBeforeOnShelf { get; set; }
}

public class CreateProductDto
{
    public required string Name { get; set; }
    public required string Barcode { get; set; }
}

public class UpdateProductDto
{
    public required string Name { get; set; }
    public required string Barcode { get; set; }
    public decimal Price { get; set; }
}
