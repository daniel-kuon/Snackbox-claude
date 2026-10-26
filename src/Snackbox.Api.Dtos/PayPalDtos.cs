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
    /// <summary>A source (mailbox or PayPal API) and the receiving admin are set.</summary>
    public bool Configured { get; set; }

    /// <summary>"mailbox", "api" or null - where payments are read from.</summary>
    public string? Source { get; set; }

    public DateTime? LastSyncAt { get; set; }
    public string? LastError { get; set; }
    public int LastAdded { get; set; }
    public int LastBooked { get; set; }
}

public class AssignPayPalPayerDto
{
    public int UserId { get; set; }

    /// <summary>
    /// Book this payer's later payments to the same user by themselves. Off for someone paying
    /// once for somebody else: then only this payment is booked.
    /// </summary>
    public bool Remember { get; set; } = true;
}

/// <summary>The Outlook.com / Microsoft 365 mailbox PayPal's "payment received" mails are read from.</summary>
public class PayPalMailboxStatusDto
{
    public string? ClientId { get; set; }
    public bool Connected { get; set; }
    public string? Account { get; set; }
}

public class PayPalMailboxClientIdDto
{
    public string ClientId { get; set; } = string.Empty;
}
