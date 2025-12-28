namespace Snackbox.Api.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Barcode { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
}
