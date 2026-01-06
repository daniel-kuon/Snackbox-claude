namespace Snackbox.Api.Models;

public class Withdrawal
{
    public int Id { get; set; }
    public int UserId { get; set; }  // Admin user who made the withdrawal
    public decimal Amount { get; set; }
    public DateTime WithdrawnAt { get; set; }
    public string? Notes { get; set; }
    public int? LinkedPaymentId { get; set; }  // For PayPal withdrawals - the corresponding payment
    
    // Navigation properties
    public User User { get; set; } = null!;
}
