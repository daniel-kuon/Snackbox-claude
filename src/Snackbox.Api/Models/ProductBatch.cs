namespace Snackbox.Api.Models;

public class ProductBatch
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public DateTime BestBeforeDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public ICollection<ShelvingAction> ShelvingActions { get; set; } = new List<ShelvingAction>();
}
