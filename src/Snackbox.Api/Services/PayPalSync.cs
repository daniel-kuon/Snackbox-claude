using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

/// <summary>
/// Fetches the payments received on the admin's PayPal account, keeps those with "Snackbox" in
/// their note or subject, and books each one whose payer is assigned to a user - as a PayPal
/// payment to the admin set in Admin → Settings (<c>PayPal:AdminUserId</c>). The rest wait on the
/// PayPal admin page until someone assigns their payer, which books them and every later one.
/// Run every <c>PayPal:PollMinutes</c> (30) by <see cref="PayPalSyncBackgroundService"/> and on demand.
/// </summary>
public class PayPalSync(ApplicationDbContext context, IConfiguration configuration, PayPalApi api,
                        PayPalSync.Status status, ILogger<PayPalSync> logger)
{
    /// <summary>What the last run did, for the admin page (one per API process).</summary>
    public class Status
    {
        public DateTime? LastSyncAt { get; set; }
        public string? LastError { get; set; }
        public int LastAdded { get; set; }
        public int LastBooked { get; set; }
    }

    // PayPal lists a transaction up to 3 hours late: every run looks a day back
    private static readonly TimeSpan Overlap = TimeSpan.FromDays(1);
    private static readonly TimeSpan FirstRun = TimeSpan.FromDays(31);

    private string Keyword => configuration["PayPal:Keyword"] is { Length: > 0 } keyword ? keyword : "Snackbox";

    public bool Configured =>
        !string.IsNullOrWhiteSpace(configuration["PayPal:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["PayPal:ClientSecret"]) &&
        configuration.GetValue<int?>("PayPal:AdminUserId") != null;

    public PayPalStatusDto GetStatus() => new()
    {
        Configured = Configured,
        LastSyncAt = status.LastSyncAt,
        LastError = status.LastError,
        LastAdded = status.LastAdded,
        LastBooked = status.LastBooked
    };

    public async Task<PayPalStatusDto> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!Configured) return GetStatus(); // the page says what is missing

        try
        {
            var now = DateTime.UtcNow;
            var newest = await context.PayPalTransactions.MaxAsync(t => (DateTime?)t.ReceivedAt, cancellationToken);
            var from = newest is { } latest ? latest - Overlap : now - FirstRun;

            var received = await api.ReceivedAsync(
                configuration.GetValue<bool>("PayPal:Sandbox") ? PayPalApi.SandboxUrl : PayPalApi.LiveUrl,
                configuration["PayPal:ClientId"]!, configuration["PayPal:ClientSecret"]!, from, now, cancellationToken);

            var added = await AddAsync(received, cancellationToken);
            var booked = await BookAssignedAsync();

            status.LastSyncAt = now;
            status.LastError = null;
            status.LastAdded = added;
            status.LastBooked = booked;
            if (added > 0 || booked > 0) logger.LogInformation("PayPal: {Added} new Snackbox payments, {Booked} booked", added, booked);
        }
        catch (Exception ex) when (ex is PayPalException or HttpRequestException or TaskCanceledException { CancellationToken.IsCancellationRequested: false })
        {
            status.LastError = ex.Message;
            logger.LogWarning("PayPal sync failed: {Message}", ex.Message);
        }
        return GetStatus();
    }

    /// <summary>The Snackbox ones not stored yet; marks stored ones PayPal took back.</summary>
    public async Task<int> AddAsync(IEnumerable<PayPalApi.Received> received, CancellationToken cancellationToken = default)
    {
        var all = received.ToList();
        // A take-back may come in the same fetch as its payment (first run, or both since the last
        // run): it marks new payments as well as stored ones - before anything is booked. Every
        // fetch reaches back past the last run, so a take-back is never outside the window.
        var takenBack = all.Where(r => r.Amount < 0 && r.ReferenceId != null)
                           .GroupBy(r => r.ReferenceId!)
                           .ToDictionary(g => g.Key, g => g.Min(r => r.At));
        foreach (var reversal in all.Where(r => r.Amount < 0 && r.ReferenceId != null))
        {
            var original = await context.PayPalTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == reversal.ReferenceId && t.ReversedAt == null, cancellationToken);
            if (original == null) continue;
            original.ReversedAt = reversal.At;
            logger.LogWarning("PayPal took back {TransactionId} ({Amount} {Currency}) - booked payment {PaymentId} needs a look",
                              original.TransactionId, original.Amount, original.Currency, original.PaymentId);
        }

        var forUs = all
            .Where(r => r.Amount > 0 && r.Note?.Contains(Keyword, StringComparison.OrdinalIgnoreCase) == true)
            .DistinctBy(r => r.TransactionId)
            .ToList();
        var ids = forUs.Select(r => r.TransactionId).ToList();
        var known = await context.PayPalTransactions
            .Where(t => ids.Contains(t.TransactionId))
            .Select(t => t.TransactionId)
            .ToListAsync(cancellationToken);

        var added = 0;
        foreach (var r in forUs.Where(r => !known.Contains(r.TransactionId)))
        {
            context.PayPalTransactions.Add(new PayPalTransaction
            {
                TransactionId = r.TransactionId,
                ReceivedAt = r.At,
                Amount = r.Amount,
                Currency = r.Currency,
                PayerKey = r.PayerKey,
                PayerName = r.PayerName,
                PayerEmail = r.PayerEmail,
                Note = r.Note,
                ReversedAt = takenBack.TryGetValue(r.TransactionId, out var reversedAt) ? reversedAt : null
            });
            added++;
        }
        await context.SaveChangesAsync(cancellationToken);
        return added;
    }

    /// <summary>Assigns the payer of <paramref name="transactionId"/> to a user and books all their open payments.</summary>
    public async Task<int> AssignAsync(int transactionId, int userId)
    {
        var transaction = await context.PayPalTransactions.FindAsync(transactionId)
                          ?? throw new KeyNotFoundException("PayPal payment not found");
        if (transaction.Currency != Currency)
            throw new PayPalException($"Paid in {transaction.Currency}, not {Currency} - book it by hand and dismiss it here");
        if (!await context.Users.AnyAsync(u => u.Id == userId)) throw new KeyNotFoundException("User not found");

        var payer = await context.PayPalPayers.FirstOrDefaultAsync(p => p.PayerKey == transaction.PayerKey);
        if (payer == null) context.PayPalPayers.Add(new PayPalPayer { PayerKey = transaction.PayerKey, UserId = userId });
        else payer.UserId = userId;
        transaction.Dismissed = false;
        await context.SaveChangesAsync();

        return await BookAssignedAsync();
    }

    /// <summary>What balances are kept in. Payments in other currencies wait for the admin.</summary>
    public const string Currency = "EUR";

    /// <summary>Books every open, not dismissed, not reversed payment in euros whose payer is assigned.</summary>
    public async Task<int> BookAssignedAsync()
    {
        var adminUserId = configuration.GetValue<int?>("PayPal:AdminUserId");
        var admin = adminUserId == null ? null : await context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId && u.IsAdmin);
        if (admin == null) throw new PayPalException("The receiving admin in Admin → Settings → PayPal is missing or no admin");

        var open = await context.PayPalTransactions
            .Where(t => t.PaymentId == null && !t.Dismissed && t.ReversedAt == null && t.Currency == Currency)
            .Join(context.PayPalPayers, t => t.PayerKey, p => p.PayerKey, (t, p) => new { Transaction = t, p.UserId })
            .OrderBy(x => x.Transaction.ReceivedAt)
            .ToListAsync();

        foreach (var item in open)
        {
            var user = await context.Users.FirstAsync(u => u.Id == item.UserId);
            var payment = new Payment
            {
                UserId = user.Id,
                Amount = item.Transaction.Amount,
                PaidAt = item.Transaction.ReceivedAt,
                Type = PaymentType.PayPal,
                AdminUserId = admin.Id,
                Notes = $"PayPal {item.Transaction.TransactionId}: {item.Transaction.Note}"
            };

            // One payment at a time, all of it or nothing (the in-memory test database has no transactions)
            await using var dbTransaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync() : null;
            await PaymentBooking.BookAsync(context, payment, user, admin);
            item.Transaction.PaymentId = payment.Id;
            await context.SaveChangesAsync();
            if (dbTransaction != null) await dbTransaction.CommitAsync();
        }
        return open.Count;
    }
}

/// <summary>Runs <see cref="PayPalSync"/> every <c>PayPal:PollMinutes</c> (30) while it is set up.</summary>
public class PayPalSyncBackgroundService(IServiceProvider services, IConfiguration configuration,
                                         ILogger<PayPalSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); // let the API start first
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = services.CreateScope();
                    var sync = scope.ServiceProvider.GetRequiredService<PayPalSync>();
                    if (sync.Configured) await sync.SyncAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A failed run must not take the API down - the next one tries again
                    logger.LogError(ex, "PayPal sync failed");
                }
                await Task.Delay(TimeSpan.FromMinutes(Math.Max(5, configuration.GetValue("PayPal:PollMinutes", 30))), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
