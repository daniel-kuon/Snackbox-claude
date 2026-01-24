namespace Snackbox.Api.Dtos;

public class BarcodeDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public required string Code { get; set; }
    public decimal Amount { get; set; }
    public bool IsLoginOnly { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBarcodeDto
{
    public int UserId { get; set; }
    public required string Code { get; set; }
    public decimal Amount { get; set; }
    public bool IsLoginOnly { get; set; }
}

public class UpdateBarcodeDto
{
    public required string Code { get; set; }
    public decimal Amount { get; set; }
    public bool IsLoginOnly { get; set; }
}
