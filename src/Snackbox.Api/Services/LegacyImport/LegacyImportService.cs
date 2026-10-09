using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.ServiceDefaults.Tracing;

namespace Snackbox.Api.Services.LegacyImport;

public interface ILegacyImportService
{
    Task<LegacyConnectionTestDto> TestAsync(LegacyConnectionDto connection, CancellationToken ct = default);
    Task<LegacyImportResultDto> ImportAsync(LegacyImportRequestDto request, CancellationToken ct = default);
    Task<LegacyVerificationDto> VerifyAsync(LegacyVerificationRequestDto request, CancellationToken ct = default);
}

[Traced]
public class LegacyImportService : ILegacyImportService
{
    /// <summary>
    /// Marks the correcting payment that carries a user's old balance over. Re-running the
    /// import replaces it instead of stacking a second one on top.
    /// </summary>
    private const string BalanceAnchorNote = "Balance carried over from the old Snackbox";

    private const string ImportedPaymentNote = "Imported from the old Snackbox";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LegacyImportService> _logger;

    public LegacyImportService(ApplicationDbContext context, IConfiguration configuration, ILogger<LegacyImportService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<LegacyConnectionTestDto> TestAsync([Sensitive] LegacyConnectionDto connection, CancellationToken ct = default) =>
        new LegacySnackboxReader(connection).TestAsync(ct);

    public async Task<LegacyImportResultDto> ImportAsync([Sensitive] LegacyImportRequestDto request, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new LegacyImportResultDto { DryRun = request.DryRun };
        var timeoutSeconds = _configuration.GetValue("Scanner:TimeoutSeconds", 60);

        var reader = new LegacySnackboxReader(request.Connection);
        var legacyUsers = await reader.GetUsersAsync(ct);
        var legacyCodes = await reader.GetCodesAsync(ct);
        var legacyScans = await reader.GetScansAsync(request.From, ct);
        var legacyPayments = await reader.GetPaymentsAsync(request.From, ct);

        _logger.LogInformation(
            "Legacy import read {Users} users, {Codes} codes, {Scans} scans, {Payments} payments from {Server}/{Database}",
            legacyUsers.Count, legacyCodes.Count, legacyScans.Count, legacyPayments.Count,
            request.Connection.Server, request.Connection.Database);

        var userMap = await MapUsersAsync(legacyUsers, result, ct);
        var barcodeMap = await MapBarcodesAsync(legacyCodes, userMap, result, ct);
        await ImportScansAsync(legacyScans, userMap, barcodeMap, timeoutSeconds, result, ct);
        await ImportPaymentsAsync(legacyPayments, userMap, result, ct);

        if (request.AnchorBalances && !request.DryRun)
        {
            await AnchorBalancesAsync(legacyUsers, userMap, result, ct);
        }

        if (request.DryRun)
        {
            // Everything so far only lives in the change tracker.
            _context.ChangeTracker.Clear();
        }
        else
        {
            await _context.SaveChangesAsync(ct);
        }

        result.DurationMs = stopwatch.ElapsedMilliseconds;
        return result;
    }

    // ---------------------------------------------------------------- users

    private async Task<Dictionary<int, User>> MapUsersAsync(List<LegacyUser> legacyUsers, LegacyImportResultDto result, CancellationToken ct)
    {
        var existing = await _context.Users.ToListAsync(ct);
        var byLegacyId = existing.Where(u => u.LegacyUserId != null).ToDictionary(u => u.LegacyUserId!.Value);
        var byName = existing.GroupBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
                             .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var usedEmails = new HashSet<string>(
            existing.Where(u => u.Email != null).Select(u => u.Email!), StringComparer.OrdinalIgnoreCase);
        var usedNames = new HashSet<string>(existing.Select(u => u.Username), StringComparer.OrdinalIgnoreCase);
        var usedCardNumbers = existing.Where(u => u.CardNumber != null).Select(u => u.CardNumber!.Value).ToHashSet();

        var map = new Dictionary<int, User>();

        foreach (var legacy in legacyUsers)
        {
            if (byLegacyId.TryGetValue(legacy.UserId, out var linked))
            {
                map[legacy.UserId] = linked;
                result.UsersMatched++;
                continue;
            }

            var name = string.IsNullOrWhiteSpace(legacy.Name) ? $"Karte {legacy.UserId}" : legacy.Name;

            // Somebody who already exists here under the same name is the same person - adopt
            // them instead of creating a duplicate (the unique username would reject it anyway).
            if (byName.TryGetValue(name, out var sameName))
            {
                sameName.LegacyUserId = legacy.UserId;
                map[legacy.UserId] = sameName;
                result.UsersMatched++;
                continue;
            }

            if (!usedNames.Add(name))
            {
                result.Warnings.Add($"Skipped legacy user {legacy.UserId}: the name {name} is already taken.");
                continue;
            }

            var email = NormaliseEmail(legacy.Email, usedEmails, legacy.UserId, result);

            // The "Karte N" users are the spare cards: the card exists, nobody has claimed it
            // yet, so it stays inactive and the kiosk offers the setup wizard on first scan.
            var isSpareCard = name.StartsWith("Karte", StringComparison.OrdinalIgnoreCase);

            var user = new User
            {
                Username = name,
                Email = email,
                IsActive = !isSpareCard,
                // "Karte 05" -> card 5, so the card wizard knows the number is taken
                CardNumber = isSpareCard ? ParseCardNumber(name, usedCardNumbers) : null,
                CreatedAt = DateTime.UtcNow,
                LegacyUserId = legacy.UserId
            };

            _context.Users.Add(user);
            map[legacy.UserId] = user;
            result.UsersCreated++;
        }

        return map;
    }

    /// <summary>The digits after "Karte", if they form a number no other card has.</summary>
    private static int? ParseCardNumber(string name, HashSet<int> used)
    {
        var digits = new string(name.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) && number > 0 && used.Add(number) ? number : null;
    }

    private static string? NormaliseEmail(string? email, HashSet<string> used, int legacyUserId, LegacyImportResultDto result)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var trimmed = email.Trim();
        if (used.Add(trimmed)) return trimmed;

        // Email is unique in the new schema; the old one shares a couple of addresses.
        result.Warnings.Add($"Legacy user {legacyUserId} imported without the e-mail {trimmed} - already used by someone else.");
        return null;
    }

    // ------------------------------------------------------------- barcodes

    private async Task<Dictionary<int, Barcode>> MapBarcodesAsync(
        List<LegacyCode> legacyCodes, Dictionary<int, User> userMap, LegacyImportResultDto result, CancellationToken ct)
    {
        var existing = await _context.Barcodes.ToListAsync(ct);
        var byLegacyId = existing.Where(b => b.LegacyCodeId != null).ToDictionary(b => b.LegacyCodeId!.Value);
        var byCode = existing.GroupBy(b => b.Code, StringComparer.OrdinalIgnoreCase)
                             .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<int, Barcode>();

        foreach (var code in legacyCodes)
        {
            if (byLegacyId.TryGetValue(code.CodeId, out var linked))
            {
                map[code.CodeId] = linked;
                continue;
            }

            if (!userMap.TryGetValue(code.UserId, out var user))
            {
                result.Warnings.Add($"Skipped legacy code {code.CodeId}: its user {code.UserId} was not imported.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(code.Code))
            {
                result.Warnings.Add($"Skipped legacy code {code.CodeId}: empty barcode.");
                continue;
            }

            if (byCode.ContainsKey(code.Code))
            {
                // The old database hands the same code to more than one user in a few places.
                // Those scans are not lost: they are keyed by CodeID, and the ones whose code
                // we could not create fall back to the user's catch-all import barcode.
                result.Warnings.Add($"Legacy code {code.CodeId} ({code.Code}) already exists - its scans go to the import barcode of {user.Username}.");
                continue;
            }

            Barcode barcode = code.IsSnackCode
                ? new PurchaseBarcode { Code = code.Code, Amount = code.Price, User = user, CreatedAt = DateTime.UtcNow, LegacyCodeId = code.CodeId }
                : new LoginBarcode { Code = code.Code, Amount = 0m, User = user, CreatedAt = DateTime.UtcNow, LegacyCodeId = code.CodeId };

            _context.Barcodes.Add(barcode);
            byCode[code.Code] = barcode;
            map[code.CodeId] = barcode;
            result.BarcodesCreated++;
        }

        return map;
    }

    /// <summary>
    /// Thousands of old scans point at codes that were deleted years ago. The money still
    /// counts, so every user gets one non-scannable catch-all barcode to hang them on.
    /// </summary>
    private async Task<Barcode> GetOrCreateImportBarcodeAsync(
        User user, Dictionary<int, Barcode> importBarcodes, LegacyImportResultDto result, CancellationToken ct)
    {
        var key = user.LegacyUserId ?? user.Id;
        if (importBarcodes.TryGetValue(key, out var cached)) return cached;

        var code = $"LEGACY-IMPORT-{key}";
        var barcode = await _context.Barcodes.FirstOrDefaultAsync(b => b.Code == code, ct);

        if (barcode == null)
        {
            barcode = new PurchaseBarcode
            {
                Code = code,
                Amount = 0m,
                User = user,
                CreatedAt = DateTime.UtcNow
            };
            _context.Barcodes.Add(barcode);
            result.BarcodesCreated++;
        }

        importBarcodes[key] = barcode;
        return barcode;
    }

    // ---------------------------------------------------------------- scans

    private async Task ImportScansAsync(
        List<LegacyScan> legacyScans, Dictionary<int, User> userMap, Dictionary<int, Barcode> barcodeMap,
        int timeoutSeconds, LegacyImportResultDto result, CancellationToken ct)
    {
        if (legacyScans.Count == 0) return;

        var alreadyImported = (await _context.BarcodeScans
                                             .Where(s => s.LegacyPostenId != null)
                                             .Select(s => s.LegacyPostenId!.Value)
                                             .ToListAsync(ct)).ToHashSet();

        var importBarcodes = new Dictionary<int, Barcode>();

        foreach (var group in legacyScans.GroupBy(s => s.UserId))
        {
            if (!userMap.TryGetValue(group.Key, out var user))
            {
                result.Warnings.Add($"Skipped {group.Count()} scans of legacy user {group.Key}: the user was not imported.");
                continue;
            }

            var pending = group.Where(s => !alreadyImported.Contains(s.PostenId)).ToList();
            result.ScansSkippedAlreadyPresent += group.Count() - pending.Count;
            if (pending.Count == 0) continue;

            // A re-run must not split a purchase that is already here: if the first new scan
            // still falls inside the window of the user's newest imported purchase, extend it.
            var openPurchase = user.Id == 0
                ? null
                : await _context.Purchases
                                .Where(p => p.UserId == user.Id && p.IsLegacyImport)
                                .OrderByDescending(p => p.UpdatedAt)
                                .FirstOrDefaultAsync(ct);

            var groups = PurchaseGrouping.Group(pending, s => s.Time, timeoutSeconds);

            for (var i = 0; i < groups.Count; i++)
            {
                var scans = groups[i];
                Purchase purchase;

                if (i == 0 && openPurchase != null &&
                    scans[0].Time >= openPurchase.UpdatedAt &&
                    scans[0].Time - openPurchase.UpdatedAt < TimeSpan.FromSeconds(timeoutSeconds))
                {
                    purchase = openPurchase;
                    purchase.UpdatedAt = scans[^1].Time;
                }
                else
                {
                    purchase = new Purchase
                    {
                        User = user,
                        CreatedAt = scans[0].Time,
                        UpdatedAt = scans[^1].Time,
                        Type = PurchaseType.Normal,
                        IsLegacyImport = true
                    };
                    _context.Purchases.Add(purchase);
                    result.PurchasesCreated++;
                }

                foreach (var scan in scans)
                {
                    var barcode = barcodeMap.TryGetValue(scan.CodeId, out var mapped)
                        ? mapped
                        : await GetOrCreateImportBarcodeAsync(user, importBarcodes, result, ct);

                    _context.BarcodeScans.Add(new BarcodeScan
                    {
                        Purchase = purchase,
                        Barcode = barcode,
                        Amount = scan.Price,
                        ScannedAt = scan.Time,
                        LegacyPostenId = scan.PostenId
                    });
                    result.ScansImported++;
                }
            }
        }
    }

    // ------------------------------------------------------------- payments

    private async Task ImportPaymentsAsync(
        List<LegacyPayment> legacyPayments, Dictionary<int, User> userMap, LegacyImportResultDto result, CancellationToken ct)
    {
        if (legacyPayments.Count == 0) return;

        var alreadyImported = (await _context.Payments
                                             .Where(p => p.LegacyToPayId != null)
                                             .Select(p => p.LegacyToPayId!.Value)
                                             .ToListAsync(ct)).ToHashSet();

        foreach (var payment in legacyPayments)
        {
            if (alreadyImported.Contains(payment.ToPayId))
            {
                result.PaymentsSkippedAlreadyPresent++;
                continue;
            }

            if (!userMap.TryGetValue(payment.UserId, out var user))
            {
                result.Warnings.Add($"Skipped a payment of legacy user {payment.UserId}: the user was not imported.");
                continue;
            }

            _context.Payments.Add(new Payment
            {
                User = user,
                Amount = payment.Amount,
                PaidAt = payment.Time,
                Type = PaymentType.CashRegister,
                Notes = ImportedPaymentNote,
                LegacyToPayId = payment.ToPayId
            });
            result.PaymentsImported++;
        }
    }

    // ------------------------------------------------------- balance anchor

    /// <summary>
    /// The old database does not reconcile with itself: T_User.rest (a debt) is not the sum of
    /// its own scans minus its payments, because the history was trimmed over the years. rest
    /// is the number people actually owe, so it wins - a correcting payment makes up the rest.
    /// </summary>
    private async Task AnchorBalancesAsync(
        List<LegacyUser> legacyUsers, Dictionary<int, User> userMap, LegacyImportResultDto result, CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);  // the sums below need the new rows to have ids

        foreach (var legacy in legacyUsers)
        {
            if (!userMap.TryGetValue(legacy.UserId, out var user)) continue;

            var previousAnchors = await _context.Payments
                                                .Where(p => p.UserId == user.Id && p.Notes == BalanceAnchorNote)
                                                .ToListAsync(ct);
            if (previousAnchors.Count > 0)
            {
                _context.Payments.RemoveRange(previousAnchors);
                await _context.SaveChangesAsync(ct);
            }

            var balance = await GetBalanceAsync(user.Id, ct);

            // rest is debt, so the balance the user should end up with is its negative.
            var delta = -legacy.Rest - balance;
            if (Math.Abs(delta) < 0.01m) continue;

            _context.Payments.Add(new Payment
            {
                UserId = user.Id,
                Amount = delta,
                PaidAt = DateTime.UtcNow,
                Type = PaymentType.CashRegister,
                Notes = BalanceAnchorNote
            });
            result.BalanceAnchorsWritten++;
            result.BalanceAnchorTotal += delta;
        }

        await _context.SaveChangesAsync(ct);
    }

    // --------------------------------------------------------- verification

    public async Task<LegacyVerificationDto> VerifyAsync([Sensitive] LegacyVerificationRequestDto request, CancellationToken ct = default)
    {
        const int maxDifferences = 500;

        var reader = new LegacySnackboxReader(request.Connection);
        var legacyUsers = await reader.GetUsersAsync(ct);
        var legacyScans = await reader.GetScansAsync(request.From, ct);
        var legacyPayments = await reader.GetPaymentsAsync(request.From, ct);

        var tolerance = TimeSpan.FromSeconds(Math.Max(0, request.MatchToleranceSeconds));
        var report = new LegacyVerificationDto
        {
            CheckedAt = DateTime.UtcNow,
            From = request.From,
            MatchToleranceSeconds = request.MatchToleranceSeconds
        };

        var users = await _context.Users
                                  .Where(u => u.LegacyUserId != null)
                                  .ToDictionaryAsync(u => u.LegacyUserId!.Value, ct);

        var scansByUser = legacyScans.GroupBy(s => s.UserId).ToDictionary(g => g.Key, g => g.ToList());
        var paymentsByUser = legacyPayments.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var legacy in legacyUsers)
        {
            var oldScans = scansByUser.GetValueOrDefault(legacy.UserId) ?? new List<LegacyScan>();
            var oldPayments = paymentsByUser.GetValueOrDefault(legacy.UserId) ?? new List<LegacyPayment>();

            var comparison = new LegacyUserComparisonDto
            {
                LegacyUserId = legacy.UserId,
                LegacyName = legacy.Name,
                OldScanCount = oldScans.Count,
                OldSpent = oldScans.Sum(s => s.Price),
                OldPaid = oldPayments.Sum(p => p.Amount),
                OldRest = legacy.Rest
            };

            if (!users.TryGetValue(legacy.UserId, out var user))
            {
                comparison.MissingInNew = oldScans.Count;
                comparison.Matches = oldScans.Count == 0 && oldPayments.Count == 0;
                report.Users.Add(comparison);
                continue;
            }

            comparison.UserId = user.Id;
            comparison.Username = user.Username;

            var newScans = await _context.BarcodeScans
                                         .Where(s => s.Purchase.UserId == user.Id &&
                                                     (request.From == null || s.ScannedAt >= request.From))
                                         .Select(s => new ScanPoint(s.ScannedAt, s.Amount))
                                         .ToListAsync(ct);

            comparison.NewScanCount = newScans.Count;
            comparison.NewSpent = newScans.Sum(s => s.ScannedAtAmount);
            // The correcting payment exists only on this side, so counting it here would make
            // every user look off by exactly the amount the import had to make up.
            comparison.NewPaid = await _context.Payments
                                               .Where(p => p.UserId == user.Id && p.Notes != BalanceAnchorNote &&
                                                           (request.From == null || p.PaidAt >= request.From))
                                               .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
            comparison.NewBalance = await GetBalanceAsync(user.Id, ct);

            // Scans the new app captured live carry no legacy id, so matching goes by what a
            // human would compare: the same amount at (roughly) the same moment.
            var matched = new bool[newScans.Count];
            foreach (var old in oldScans)
            {
                var index = -1;
                for (var i = 0; i < newScans.Count; i++)
                {
                    if (matched[i] || newScans[i].ScannedAtAmount != old.Price) continue;
                    if ((newScans[i].ScannedAt - old.Time).Duration() > tolerance) continue;
                    index = i;
                    break;
                }

                if (index >= 0)
                {
                    matched[index] = true;
                    continue;
                }

                comparison.MissingInNew++;
                AddDifference(report, maxDifferences, new LegacyScanDiffDto
                {
                    LegacyUserId = legacy.UserId,
                    UserName = legacy.Name,
                    ScannedAt = old.Time,
                    Amount = old.Price,
                    Side = "OnlyInOld"
                });
            }

            for (var i = 0; i < newScans.Count; i++)
            {
                if (matched[i]) continue;

                comparison.MissingInOld++;
                AddDifference(report, maxDifferences, new LegacyScanDiffDto
                {
                    LegacyUserId = legacy.UserId,
                    UserName = user.Username,
                    ScannedAt = newScans[i].ScannedAt,
                    Amount = newScans[i].ScannedAtAmount,
                    Side = "OnlyInNew"
                });
            }

            comparison.Matches = comparison.MissingInNew == 0 && comparison.MissingInOld == 0 &&
                                 Math.Abs(comparison.NewBalance + comparison.OldRest) < 0.01m;

            report.Users.Add(comparison);
        }

        report.UsersCompared = report.Users.Count;
        report.UsersMatching = report.Users.Count(u => u.Matches);
        report.TotalMissingInNew = report.Users.Sum(u => u.MissingInNew);
        report.TotalMissingInOld = report.Users.Sum(u => u.MissingInOld);
        report.OldSpentTotal = report.Users.Sum(u => u.OldSpent);
        report.NewSpentTotal = report.Users.Sum(u => u.NewSpent);
        report.OldPaidTotal = report.Users.Sum(u => u.OldPaid);
        report.NewPaidTotal = report.Users.Sum(u => u.NewPaid);
        report.Differences = report.Differences.OrderByDescending(d => d.ScannedAt).ToList();
        return report;
    }

    private record ScanPoint(DateTime ScannedAt, decimal ScannedAtAmount);

    private static void AddDifference(LegacyVerificationDto report, int max, LegacyScanDiffDto difference)
    {
        if (report.Differences.Count >= max)
        {
            report.DifferencesTruncated = true;
            return;
        }

        report.Differences.Add(difference);
    }

    private async Task<decimal> GetBalanceAsync(int userId, CancellationToken ct)
    {
        var spent = await _context.BarcodeScans.Where(s => s.Purchase.UserId == userId)
                                  .SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        var manual = await _context.Purchases.Where(p => p.UserId == userId && p.ManualAmount != null)
                                   .SumAsync(p => (decimal?)p.ManualAmount, ct) ?? 0m;
        var paid = await _context.Payments.Where(p => p.UserId == userId)
                                 .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        return paid - spent - manual;
    }
}
