namespace Snackbox.Api.Dtos;

public class PurchaseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
}

public class PurchaseItemDto
{
    public int Id { get; set; }
    public string? ProductName { get; set; }
    public decimal Amount { get; set; }
    public DateTime ScannedAt { get; set; }
}

public class StartPurchaseDto
{
    public required string ProductBarcode { get; set; }
}

public class AddToPurchaseDto
{
    public required string ProductBarcode { get; set; }
}
