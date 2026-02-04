using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

/// <summary>
/// Service for calculating user balances consistently across the application
/// </summary>
public class BalanceCalculationService : IBalanceCalculationService
{
    /// <inheritdoc/>
    public decimal CalculateBalance(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        return CalculateBalance(user.Payments, user.Purchases, user.Withdrawals);
    }

    /// <inheritdoc/>
    public decimal CalculateBalance(
        IEnumerable<Payment> payments,
        IEnumerable<Purchase> purchases,
        IEnumerable<Withdrawal> withdrawals)
    {
        var totalPaid = payments?.Sum(p => p.Amount) ?? 0;
        var totalSpent = purchases?.Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount)) ?? 0;
        var totalWithdrawn = withdrawals?.Sum(w => w.Amount) ?? 0;

        // Balance = What they've paid - What they've spent - What they've withdrawn
        // Positive = user has credit, Negative = user owes money
        return totalPaid - totalSpent - totalWithdrawn;
    }
}
