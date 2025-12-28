namespace Snackbox.Api.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Barcode { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductDto
{
    public required string Name { get; set; }
    public required string Barcode { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
}

public class UpdateProductDto
{
    public required string Name { get; set; }
    public required string Barcode { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
}
