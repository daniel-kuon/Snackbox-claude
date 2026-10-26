using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

/// <summary>
/// Books a payment with everything that goes with it - the admin's entry in Payments and the PayPal
/// sync both book through here. Every payment gets a deposit on the payer; a PayPal payment also a
/// withdrawal on the admin whose PayPal account received the money (it is theirs to hand over);
/// cash goes into the cash register. The caller owns the transaction.
/// </summary>
public static class PaymentBooking
{
    public static async Task BookAsync(ApplicationDbContext context, Payment payment, User user, User? adminUser)
    {
        Withdrawal? withdrawal = null;
        if (payment.Type == PaymentType.PayPal && adminUser != null)
        {
            withdrawal = new Withdrawal
            {
                UserId = adminUser.Id,
                Amount = payment.Amount,
                WithdrawnAt = DateTime.UtcNow,
                Notes = $"PayPal payment from {user.Username}"
            };
            context.Withdrawals.Add(withdrawal);
        }

        var deposit = new Deposit
        {
            UserId = user.Id,
            Amount = payment.Amount,
            DepositedAt = DateTime.UtcNow,
        };
        context.Deposits.Add(deposit);

        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        if (withdrawal != null)
        {
            payment.LinkedWithdrawalId = withdrawal.Id;
            withdrawal.LinkedPaymentId = payment.Id;
        }

        payment.LinkedDepositId = deposit.Id;
        deposit.LinkedPaymentId = payment.Id;

        if (payment.Type == PaymentType.CashRegister)
        {
            var cashRegister = await context.CashRegister.FirstOrDefaultAsync();
            if (cashRegister == null)
            {
                context.CashRegister.Add(new CashRegister
                {
                    CurrentBalance = payment.Amount,
                    LastUpdatedAt = DateTime.UtcNow,
                    LastUpdatedByUserId = user.Id
                });
            }
            else
            {
                cashRegister.CurrentBalance += payment.Amount;
                cashRegister.LastUpdatedAt = DateTime.UtcNow;
                cashRegister.LastUpdatedByUserId = user.Id;
            }
        }

        await context.SaveChangesAsync();
    }
}
