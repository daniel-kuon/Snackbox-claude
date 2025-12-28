namespace Snackbox.Api.DTOs;

public class ProductBatchDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public DateTime BestBeforeDate { get; set; }
    public int QuantityInStorage { get; set; }
    public int QuantityOnShelf { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductBatchDto
{
    public int ProductId { get; set; }
    public DateTime BestBeforeDate { get; set; }
    public int InitialQuantity { get; set; }
}

public class UpdateProductBatchDto
{
    public DateTime BestBeforeDate { get; set; }
}

public class MoveStockDto
{
    public int Quantity { get; set; }
}
