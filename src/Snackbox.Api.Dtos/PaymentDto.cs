namespace Snackbox.Api.Dtos;

public class PaymentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
    public string Type { get; set; } = "CashRegister";
    public int? AdminUserId { get; set; }
    public string? AdminUsername { get; set; }
    public int? LinkedWithdrawalId { get; set; }
}

public class CreatePaymentDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string Type { get; set; } = "CashRegister";
    public int? AdminUserId { get; set; }
}

public class WithdrawalDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public decimal Amount { get; set; }
    public DateTime WithdrawnAt { get; set; }
    public string? Notes { get; set; }
    public int? LinkedPaymentId { get; set; }
}

public class CreateWithdrawalDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class CashRegisterDto
{
    public int Id { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public int LastUpdatedByUserId { get; set; }
    public string? LastUpdatedByUsername { get; set; }
}
