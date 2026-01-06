namespace Snackbox.Api.Models;

public class Deposit
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DepositedAt { get; set; }
    public int? LinkedPaymentId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Payment? LinkedPayment { get; set; }
}
