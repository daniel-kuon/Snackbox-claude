namespace Snackbox.Api.DTOs;

public class ScanBarcodeResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // User information
    public int UserId { get; set; }
    public required string Username { get; set; }
    public bool IsAdmin { get; set; }

    // Current purchase information
    public int PurchaseId { get; set; }
    public List<ScannedBarcodeDto> ScannedBarcodes { get; set; } = new();
    public decimal TotalAmount { get; set; }

    // User financial information
    public decimal Balance { get; set; } // Total spent - total paid (negative = owes money)
    public decimal LastPaymentAmount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
}

public class ScannedBarcodeDto
{
    public string BarcodeCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ScannedAt { get; set; }
}
