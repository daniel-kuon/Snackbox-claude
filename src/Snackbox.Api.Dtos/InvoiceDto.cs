namespace Snackbox.Api.Dtos;

public enum InvoiceType
{
    ManualSimple = 0,
    ManualDetailed = 1,
    Parsed = 2
}

public class InvoiceDto
{
    public int Id { get; set; }
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public InvoiceType Type { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AdditionalCosts { get; set; }
    public decimal PriceReduction { get; set; }
    public int PaidByUserId { get; set; }
    public string? PaidByUsername { get; set; }
    public int? PaymentId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByUsername { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
    public bool HasUnprocessedItems { get; set; }
}

public class InvoiceItemDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public string? MatchedProductName { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime? BestBeforeDate { get; set; }
    public string? Notes { get; set; }
    public string? ArticleNumber { get; set; }
    public InvoiceItemStatus Status { get; set; }
    public ShelvingActionType? ActionType { get; set; }
}

public class CreateInvoiceDto
{
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal AdditionalCosts { get; set; }
    public decimal PriceReduction { get; set; }
    public int PaidByUserId { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItemDto> Items { get; set; } = new();
}

public class CreateInvoiceItemDto
{
    public int? ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? BestBeforeDate { get; set; }
    public string? Notes { get; set; }
    public string? ArticleNumber { get; set; }
}

public class UpdateInvoiceDto
{
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal AdditionalCosts { get; set; }
    public decimal PriceReduction { get; set; }
    public int PaidByUserId { get; set; }
    public string? Notes { get; set; }
}

public class ParseInvoiceRequest
{
    public required string InvoiceText { get; set; }
    public required string Format { get; set; } // "sonderposten" or "selgros"
}

public class ParseInvoiceResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ParsedInvoiceItem> Items { get; set; } = new();
    public InvoiceMetadata? Metadata { get; set; }
}

public class ParsedInvoiceItem
{
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime? BestBeforeDate { get; set; }
    public string? ArticleNumber { get; set; }
    public bool Selected { get; set; } = true;
    public int? MatchedProductId { get; set; }
    public string? MatchedProductName { get; set; }
    public string? MatchType { get; set; }
    public decimal? MatchConfidence { get; set; }
}

public class InvoiceMetadata
{
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Supplier { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? AdditionalCosts { get; set; }
    public decimal? PriceReduction { get; set; }
    public string? Notes { get; set; }
}

public class CreateInvoiceFromParsedDto
{
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal AdditionalCosts { get; set; }
    public decimal PriceReduction { get; set; }
    public int PaidByUserId { get; set; }
    public string? Notes { get; set; }
    public List<ParsedInvoiceItem> SelectedItems { get; set; } = new();
}

public class AddInvoiceItemToStockDto
{
    public int? ProductId { get; set; }
    public string? ProductBarcode { get; set; }
    public bool AddToShelf { get; set; }
}

public class UpdateInvoiceItemDto
{
    public DateTime? BestBeforeDate { get; set; }
}

public class CreateManualSimpleInvoiceDto
{
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal TotalAmount { get; set; }
    public int PaidByUserId { get; set; }
    public string? Notes { get; set; }
}

public class CreateManualDetailedInvoiceDto
{
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal AdditionalCosts { get; set; }
    public decimal PriceReduction { get; set; }
    public int PaidByUserId { get; set; }
    public string? Notes { get; set; }
    public List<ManualInvoiceItemDto> Items { get; set; } = new();
}

public class ManualInvoiceItemDto
{
    public int? ProductId { get; set; }
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? BestBeforeDate { get; set; }
}
