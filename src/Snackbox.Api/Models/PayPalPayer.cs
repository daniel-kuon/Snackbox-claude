namespace Snackbox.Api.Models;

/// <summary>
/// A PayPal payer the admin assigned to a user: their next Snackbox payments are booked
/// without anyone looking.
/// </summary>
public class PayPalPayer
{
    public int Id { get; set; }

    /// <summary>Same key as <see cref="PayPalTransaction.PayerKey"/>.</summary>
    public required string PayerKey { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
