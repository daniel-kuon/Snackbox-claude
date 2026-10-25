namespace Snackbox.Api.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Price per item on the tablet. Products without one are not offered there.</summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Compartment of the 3x3 shelf, numbered 1-9 row by row from the top left. 2 (top middle)
    /// holds the tablet, so it is never a product's. The tablet lays its screen out the same way.
    /// </summary>
    public int? ShelfSlot { get; set; }

    // Navigation properties
    public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
    public ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
    public DateTime? BestBeforeInStock { get; set; }
    public DateTime? BestBeforeOnShelf { get; set; }
    public ProductImage? Image { get; set; }
}
