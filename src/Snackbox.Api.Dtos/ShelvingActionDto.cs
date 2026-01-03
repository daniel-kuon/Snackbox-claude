using Snackbox.Api.Models;

namespace Snackbox.Api.DTOs;

public class ShelvingActionDto
{
    public int Id { get; set; }
    public int ProductBatchId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductBarcode { get; set; } = string.Empty;
    public DateTime BestBeforeDate { get; set; }
    public int Quantity { get; set; }
    public required ShelvingActionType Type { get; set; }
    public DateTime ActionAt { get; set; }
}

public class CreateShelvingActionDto
{
    public string ProductBarcode { get; set; } = string.Empty;
    public DateTime BestBeforeDate { get; set; }
    public int Quantity { get; set; }
    public ShelvingActionType Type { get; set; }
}

public class BatchShelvingRequest
{
    public List<CreateShelvingActionDto> Actions { get; set; } = new();
}

public class ProductStockDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductBarcode { get; set; } = string.Empty;
    public List<ProductBarcodeDto> Barcodes { get; set; } = new();
    public int TotalInStorage { get; set; }
    public int TotalOnShelf { get; set; }
    public List<BatchStockInfo> Batches { get; set; } = new();
}

public class BatchStockInfo
{
    public int BatchId { get; set; }
    public DateTime BestBeforeDate { get; set; }
    public int QuantityInStorage { get; set; }
    public int QuantityOnShelf { get; set; }
}
