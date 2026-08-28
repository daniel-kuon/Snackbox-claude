namespace Snackbox.Api.Dtos;

/// <summary>
/// Completes the setup of an inactive (placeholder) account from the kiosk.
/// The scanned barcode code acts as proof of card possession.
/// </summary>
public class CompleteAccountSetupDto
{
    public required string BarcodeCode { get; set; }
    public required string Username { get; set; }
    public string? Email { get; set; }
}

public class MarkIntroSeenDto
{
    public required string BarcodeCode { get; set; }
}

public class CompleteAccountSetupResponse
{
    public required string Username { get; set; }
}
