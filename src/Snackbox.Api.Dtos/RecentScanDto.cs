namespace Snackbox.Api.Dtos;

/// <summary>One kiosk scan for the admin dashboard. <see cref="UserId"/> is null when the code was not found.</summary>
public class RecentScanDto
{
    public DateTime ScannedAt { get; set; }
    public required string Code { get; set; }
    public decimal? Amount { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
}
