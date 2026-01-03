namespace Snackbox.Api.Dtos;

public class SetPasswordRequest
{
    public required string BarcodeValue { get; set; }
    public required string Email { get; set; }
    public required string NewPassword { get; set; }
}
