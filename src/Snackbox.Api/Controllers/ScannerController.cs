using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScannerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private const int DefaultTimeoutSeconds = 60;

    public ScannerController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("scan")]
    public async Task<ActionResult<ScanBarcodeResponse>> ScanBarcode([FromBody] ScanBarcodeRequest request)
    {
        // Get timeout from configuration
        var timeoutSeconds = _configuration.GetValue("Scanner:TimeoutSeconds", DefaultTimeoutSeconds);
        var timeoutThreshold = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        // Find the barcode
        var barcode = await _context.Barcodes
            .Include(b => b.User)
                .ThenInclude(u => u.Payments)
            .FirstOrDefaultAsync(b => b.Code == request.BarcodeCode);

        if (barcode == null)
        {
            return Ok(new ScanBarcodeResponse
            {
                Success = false,
                ErrorMessage = "Barcode not found",
                UserId = 0,
                Username = string.Empty
            });
        }

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

        // Check if this is a login-only barcode
        if (barcode.IsLoginOnly)
        {
            // Login-only barcodes cannot be used for purchases
            return Ok(new ScanBarcodeResponse
            {
                Success = false,
                ErrorMessage = "This barcode is for login only and cannot be used for purchases",
                UserId = barcode.UserId,
                Username = barcode.User.Username
            });
        }

        var user = barcode.User;

        // Find the last incomplete purchase for this user
        var lastPurchase = await _context.Purchases
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .Where(p => p.UserId == user.Id && p.CompletedAt == null)
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
            .Where(p => p.UserId == user.Id && p.CompletedAt != null && p.Id != currentPurchase.Id)
            .OrderByDescending(p => p.CompletedAt)
            .Take(3)
            .Select(p => new RecentPurchaseDto
            {
                PurchaseId = p.Id,
                TotalAmount = p.Scans.Sum(s => s.Amount),
                CompletedAt = p.CompletedAt!.Value,
                ItemCount = p.Scans.Count
            })
            .ToListAsync();

        // Log for debugging
        Console.WriteLine($"User {user.Id} - Found {recentPurchases.Count} recent purchases");

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
            RecentPurchases = recentPurchases
        };

        return Ok(response);
    }
}
