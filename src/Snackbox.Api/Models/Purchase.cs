namespace Snackbox.Api.Models;

public class Purchase
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<BarcodeScan> Scans { get; set; } = new List<BarcodeScan>();
}
