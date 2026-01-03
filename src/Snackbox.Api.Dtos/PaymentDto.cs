namespace Snackbox.Api.Dtos;

public class PaymentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
}

public class CreatePaymentDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
