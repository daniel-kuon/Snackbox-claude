using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

/// <summary>
/// Service for calculating user balances
/// </summary>
public interface IBalanceCalculationService
{
    /// <summary>
    /// Calculates the balance for a user based on their payments, purchases, and withdrawals.
    /// Balance = Payments - Purchases - Withdrawals
    /// A positive balance means the user has credit, negative means they owe money.
    /// </summary>
    /// <param name="user">The user with loaded Payments, Purchases (with Scans), and Withdrawals</param>
    /// <returns>The calculated balance</returns>
    decimal CalculateBalance(User user);

    /// <summary>
    /// Calculates the balance using preloaded collections
    /// </summary>
    decimal CalculateBalance(
        IEnumerable<Payment> payments,
        IEnumerable<Purchase> purchases,
        IEnumerable<Withdrawal> withdrawals);
}
