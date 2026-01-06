namespace Snackbox.Api.Models;

public class CashRegister
{
    public int Id { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public int LastUpdatedByUserId { get; set; }
    
    // Navigation properties
    public User LastUpdatedByUser { get; set; } = null!;
}
