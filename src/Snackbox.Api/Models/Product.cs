namespace Snackbox.Api.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
    public ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
    public DateTime? BestBeforeInStock { get; set; }
    public DateTime? BestBeforeOnShelf { get; set; }
}
