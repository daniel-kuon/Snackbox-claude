using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScannerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAchievementService _achievementService;
    private readonly ILogger<ScannerController> _logger;
    private const int DefaultTimeoutSeconds = 60;

    public ScannerController(ApplicationDbContext context, IConfiguration configuration, IAchievementService achievementService, ILogger<ScannerController> logger)
    {
        _context = context;
        _configuration = configuration;
        _achievementService = achievementService;
        _logger = logger;
    }

    [HttpPost("scan")]
    public async Task<ActionResult<ScanBarcodeResponse>> ScanBarcode([FromBody] ScanBarcodeRequest request)
    {
        _logger.LogInformation("Scanning barcode: {BarcodeCode}", request.BarcodeCode);

        // Get timeout from configuration
        var timeoutSeconds = _configuration.GetValue("Scanner:TimeoutSeconds", DefaultTimeoutSeconds);
        var timeoutThreshold = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        // Find the barcode - use AsNoTracking to avoid caching issues
        var barcode = await _context.Barcodes
            .AsNoTracking()
            .Include(b => b.User)
                .ThenInclude(u => u.Payments)
            .FirstOrDefaultAsync(b => b.Code == request.BarcodeCode);

        if (barcode == null)
        {
            _logger.LogWarning("Barcode not found in database: {BarcodeCode}", request.BarcodeCode);

            // Check if database has any barcodes at all - log some for debugging
            var totalBarcodes = await _context.Barcodes.CountAsync();
            var allBarcodeCodes = await _context.Barcodes
                .AsNoTracking()
                .Select(b => b.Code)
                .Take(10)
                .ToListAsync();
            _logger.LogInformation("Total barcodes in database: {Count}. First few: {Codes}",
                totalBarcodes, string.Join(", ", allBarcodeCodes));

            return Ok(new ScanBarcodeResponse
            {
                Success = false,
                ErrorMessage = "Barcode not found",
                UserId = 0,
                Username = string.Empty
            });
        }

        _logger.LogInformation("Barcode found: {BarcodeId}, User: {UserId}, Type: {Type}",
            barcode.Id, barcode.UserId, barcode is LoginBarcode ? "Login" : "Purchase");

        if (!barcode.IsActive)
        {
            return Ok(new ScanBarcodeResponse
            {
                Success = false,
                ErrorMessage = "Barcode is inactive",
                UserId = 0,
                Username = string.Empty
            });
        }

        var user = barcode.User;

        // Check if this is a login barcode - return success with user info but don't create a purchase
        if (barcode is LoginBarcode)
        {
            return Ok(new ScanBarcodeResponse
            {
                Success = true,
                UserId = user.Id,
                Username = user.Username,
                IsAdmin = user.IsAdmin,
                IsLoginOnly = true
            });
        }

        // Find the last incomplete purchase for this user
        var lastPurchase = await _context.Purchases
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        Purchase currentPurchase;
        bool isNewPurchase = false;

        // Check if we should add to existing purchase or create new one
        if (lastPurchase != null)
        {
            var lastScan = lastPurchase.Scans
                .OrderByDescending(s => s.ScannedAt)
                .FirstOrDefault();

            // If last scan was within timeout window, add to existing purchase
            if (lastScan != null && lastScan.ScannedAt >= timeoutThreshold)
            {
                currentPurchase = lastPurchase;
            }
            else
            {
                // Timeout expired - complete the old purchase and create a new one
                if (lastPurchase.Scans.Any())
                {
                    // Use the last scan time as the completion time (more accurate than "now")
                    lastPurchase.CompletedAt = lastScan?.ScannedAt ?? DateTime.UtcNow;

                    // Save the completion first
                    await _context.SaveChangesAsync();

                    // Check for achievements earned from the completed purchase
                    await _achievementService.CheckAndAwardAchievementsAsync(user.Id, lastPurchase.Id);
                }
                else
                {
                    // Remove empty purchase (shouldn't happen, but handle it)
                    _context.Purchases.Remove(lastPurchase);
                }

                currentPurchase = new Purchase
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Purchases.Add(currentPurchase);
                isNewPurchase = true;
            }
        }
        else
        {
            // No existing incomplete purchase, create new one
            currentPurchase = new Purchase
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Purchases.Add(currentPurchase);
            isNewPurchase = true;
        }

        // Add the barcode scan to the purchase
        var barcodeScan = new BarcodeScan
        {
            Purchase = currentPurchase,
            BarcodeId = barcode.Id,
            Amount = barcode.Amount,
            ScannedAt = DateTime.UtcNow
        };
        _context.BarcodeScans.Add(barcodeScan);

        await _context.SaveChangesAsync();

        // Reload purchase with all scans to get updated data
        if (isNewPurchase)
        {
            await _context.Entry(currentPurchase)
                .Collection(p => p.Scans)
                .Query()
                .Include(s => s.Barcode)
                .LoadAsync();
        }

        // Check for immediate achievements (single purchase amount, high debt, total spent)
        // These can be checked on every scan, not just when the purchase completes
        await _achievementService.CheckImmediateAchievementsAsync(user.Id, currentPurchase.Id);

        // Calculate user's balance (total spent - total paid)
        var totalSpent = await _context.BarcodeScans
            .Where(bs => bs.Purchase.UserId == user.Id)
            .SumAsync(bs => bs.Amount);

        var totalPaid = await _context.Payments
            .Where(p => p.UserId == user.Id)
            .SumAsync(p => p.Amount);

        var balance = totalSpent - totalPaid;

        // Get last payment
        var lastPayment = await _context.Payments
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefaultAsync();

        // Get last 3 completed purchases (excluding the current one)
        var recentPurchases = await _context.Purchases
            .Include(p => p.Scans)
            .Where(p => p.UserId == user.Id && p.Id != currentPurchase.Id)
            .OrderByDescending(p => p.CompletedAt)
            .Take(3)
            .Select(p => new RecentPurchaseDto
            {
                PurchaseId = p.Id,
                TotalAmount = p.Scans.Sum(s => s.Amount),
                CompletedAt = p.CompletedAt,
                ItemCount = p.Scans.Count
            })
            .ToListAsync();

        // Log for debugging
        Console.WriteLine($"User {user.Id} - Found {recentPurchases.Count} recent purchases");

        // Get newly earned achievements (not yet shown to user)
        var newAchievements = await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == user.Id && !ua.HasBeenShown)
            .Select(ua => new AchievementDto
            {
                Id = ua.Achievement.Id,
                Code = ua.Achievement.Code,
                Name = ua.Achievement.Name,
                Description = ua.Achievement.Description,
                Category = ua.Achievement.Category.ToString(),
                ImageUrl = ua.Achievement.ImageUrl,
                EarnedAt = ua.EarnedAt
            })
            .ToListAsync();

        Console.WriteLine($"User {user.Id} - Found {newAchievements.Count} new achievements to show");

        // Mark achievements as shown
        if (newAchievements.Any())
        {
            var achievementIds = newAchievements.Select(a => a.Id).ToList();
            var userAchievementsToUpdate = await _context.UserAchievements
                .Where(ua => ua.UserId == user.Id && achievementIds.Contains(ua.AchievementId))
                .ToListAsync();

            foreach (var ua in userAchievementsToUpdate)
            {
                ua.HasBeenShown = true;
            }
            await _context.SaveChangesAsync();
            Console.WriteLine($"User {user.Id} - Marked {userAchievementsToUpdate.Count} achievements as shown");
        }

        // Build response
        var response = new ScanBarcodeResponse
        {
            Success = true,
            UserId = user.Id,
            Username = user.Username,
            IsAdmin = user.IsAdmin,
            PurchaseId = currentPurchase.Id,
            ScannedBarcodes = currentPurchase.Scans
                .OrderBy(s => s.ScannedAt)
                .Select(s => new ScannedBarcodeDto
                {
                    BarcodeCode = s.Barcode.Code,
                    Amount = s.Amount,
                    ScannedAt = s.ScannedAt
                })
                .ToList(),
            TotalAmount = currentPurchase.Scans.Sum(s => s.Amount),
            Balance = balance,
            LastPaymentAmount = lastPayment?.Amount ?? 0,
            LastPaymentDate = lastPayment?.PaidAt,
            RecentPurchases = recentPurchases,
            NewAchievements = newAchievements
        };

        return Ok(response);
    }
}
