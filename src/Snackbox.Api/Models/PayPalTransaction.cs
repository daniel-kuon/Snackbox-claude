namespace Snackbox.Api.Models;

/// <summary>
/// A payment received on the admin's PayPal account with "Snackbox" in its note or subject,
/// fetched by <see cref="Services.PayPalSync"/>. Booked as a <see cref="Payment"/> once its
/// payer is assigned to a user - right away when the payer was assigned before.
/// </summary>
public class PayPalTransaction
{
    public int Id { get; set; }

    /// <summary>PayPal's transaction id - what makes a re-fetch skip it.</summary>
    public required string TransactionId { get; set; }

    public DateTime ReceivedAt { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }

    /// <summary>PayPal's account id of the payer, or their e-mail when PayPal sends none - see <see cref="PayPalPayer"/>.</summary>
    public required string PayerKey { get; set; }
    public string? PayerName { get; set; }
    public string? PayerEmail { get; set; }

    /// <summary>Subject and note the payer wrote.</summary>
    public string? Note { get; set; }

    /// <summary>The booked payment; null while the payer is unknown.</summary>
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    /// <summary>Not for Snackbox after all (or booked by hand): no longer listed as open.</summary>
    public bool Dismissed { get; set; }
}
