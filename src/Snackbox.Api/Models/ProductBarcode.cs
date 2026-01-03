namespace Snackbox.Api.Models;

public class ProductBarcode
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string Barcode { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
}
