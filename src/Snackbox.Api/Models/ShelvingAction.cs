namespace Snackbox.Api.Models;

public class ShelvingAction
{
    public int Id { get; set; }
    public int ProductBatchId { get; set; }
    public int Quantity { get; set; }
    public ShelvingActionType Type { get; set; }
    public DateTime ActionAt { get; set; }

    // Navigation properties
    public ProductBatch ProductBatch { get; set; } = null!;
}

public enum ShelvingActionType
{
    AddedToStorage,
    MovedToShelf,
    MovedFromShelf,
    RemovedFromStorage,
    RemovedFromShelf
}
