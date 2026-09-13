namespace Snackbox.Api.Dtos;

/// <summary>
/// DTOs for the development-only UI test helper.
/// </summary>
public class TestUserDto
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public List<TestBarcodeDto> Barcodes { get; set; } = new();
}

public class TestBarcodeDto
{
    public required string Code { get; set; }
    public decimal Amount { get; set; }
    public bool IsLoginBarcode { get; set; }
}

public class TestLoginRequest
{
    public int UserId { get; set; }
}
