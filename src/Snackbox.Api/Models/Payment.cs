namespace Snackbox.Api.Models;

public class Payment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
    public PaymentType Type { get; set; } = PaymentType.CashRegister;
    public int? AdminUserId { get; set; }  // For PayPal payments - which admin received it
    public int? LinkedWithdrawalId { get; set; }  // For PayPal payments - the corresponding withdrawal
    public int? LinkedDepositId { get; set; }  // For CashRegister payments - the corresponding deposit
    public int? InvoiceId { get; set; }  // Link to invoice if payment is for an invoice

    // Navigation properties
    public User User { get; set; } = null!;
    public User? AdminUser { get; set; }  // Admin who received PayPal payment
    public Deposit? LinkedDeposit { get; set; }  // Linked deposit for cash register payments
    public Invoice? Invoice { get; set; }  // Linked invoice
}
