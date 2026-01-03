namespace Snackbox.Api.Dtos;

public class InvoiceDto
{
    public int Id { get; set; }
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AdditionalCosts { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByUsername { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime? BestBeforeDate { get; set; }
    public string? Notes { get; set; }
    public string? ArticleNumber { get; set; }
}

public class CreateInvoiceDto
{
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal AdditionalCosts { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItemDto> Items { get; set; } = new();
}

public class CreateInvoiceItemDto
{
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
}

public class InvoiceMetadata
{
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Supplier { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? AdditionalCosts { get; set; }
}

public class CreateInvoiceFromParsedDto
{
    public required string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public required string Supplier { get; set; }
    public decimal AdditionalCosts { get; set; }
    public string? Notes { get; set; }
    public List<ParsedInvoiceItem> SelectedItems { get; set; } = new();
}
