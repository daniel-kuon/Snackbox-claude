namespace Snackbox.Api.Dtos;

public class DepositDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DepositedAt { get; set; }
    public int? LinkedPaymentId { get; set; }
}
