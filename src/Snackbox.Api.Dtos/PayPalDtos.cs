namespace Snackbox.Api.Dtos;

/// <summary>A received PayPal payment with "Snackbox" in its note, as the admin page lists it.</summary>
public class PayPalTransactionDto
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? PayerName { get; set; }
    public string? PayerEmail { get; set; }
    public string? Note { get; set; }

    /// <summary>Booked as this payment, to <see cref="BookedUsername"/>.</summary>
    public int? PaymentId { get; set; }
    public string? BookedUsername { get; set; }

    /// <summary>The user this payer is assigned to, if any.</summary>
    public int? AssignedUserId { get; set; }

    public bool Dismissed { get; set; }

    /// <summary>PayPal took the money back - see the booked payment.</summary>
    public DateTime? ReversedAt { get; set; }
}

public class PayPalStatusDto
{
    /// <summary>Client id, secret and the receiving admin are set.</summary>
    public bool Configured { get; set; }

    public DateTime? LastSyncAt { get; set; }
    public string? LastError { get; set; }
    public int LastAdded { get; set; }
    public int LastBooked { get; set; }
}

public class AssignPayPalPayerDto
{
    public int UserId { get; set; }
}
