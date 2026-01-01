using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;
using Snackbox.Api.Telemetry;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PurchasesController> _logger;

    public PurchasesController(ApplicationDbContext context, ILogger<PurchasesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("my-purchases")]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetMyPurchases()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetMyPurchases");
        
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to GetMyPurchases");
            return Unauthorized();
        }

        activity?.SetTag("user.id", userId.Value);
        _logger.LogInformation("Fetching purchases for user. UserId: {UserId}", userId.Value);

        var purchases = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .Where(p => p.UserId == userId.Value && p.CompletedAt != null)
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync();

        var dtos = purchases.Select(p => new PurchaseDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Username = p.User.Username,
            TotalAmount = p.Scans.Sum(s => s.Amount),
            CreatedAt = p.CreatedAt,
            CompletedAt = p.CompletedAt,
            Items = p.Scans.Select(s => new PurchaseItemDto
            {
                Id = s.Id,
                ProductName = s.Barcode.Code,
                Amount = s.Amount,
                ScannedAt = s.ScannedAt
            }).ToList()
        }).ToList();

        _logger.LogInformation(
            "Retrieved {PurchaseCount} completed purchases for user. UserId: {UserId}, TotalAmount: {TotalAmount}",
            dtos.Count,
            userId.Value,
            dtos.Sum(p => p.TotalAmount));

        activity?.SetTag("purchases.count", dtos.Count);
        activity?.SetTag("purchases.total_amount", dtos.Sum(p => p.TotalAmount));

        return Ok(dtos);
    }

    [HttpGet("my-purchases/current")]
    public async Task<ActionResult<PurchaseDto?>> GetCurrentPurchase()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetCurrentPurchase");
        
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to GetCurrentPurchase");
            return Unauthorized();
        }

        activity?.SetTag("user.id", userId.Value);
        _logger.LogInformation("Fetching current purchase for user. UserId: {UserId}", userId.Value);

        var purchase = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CompletedAt == null);

        if (purchase == null)
        {
            _logger.LogInformation("No current purchase found for user. UserId: {UserId}", userId.Value);
            activity?.SetTag("purchase.exists", false);
            return Ok(null);
        }

        var totalAmount = purchase.Scans.Sum(s => s.Amount);
        var dto = new PurchaseDto
        {
            Id = purchase.Id,
            UserId = purchase.UserId,
            Username = purchase.User.Username,
            TotalAmount = totalAmount,
            CreatedAt = purchase.CreatedAt,
            CompletedAt = purchase.CompletedAt,
            Items = purchase.Scans.Select(s => new PurchaseItemDto
            {
                Id = s.Id,
                ProductName = s.Barcode.Code,
                Amount = s.Amount,
                ScannedAt = s.ScannedAt
            }).ToList()
        };

        _logger.LogInformation(
            "Current purchase retrieved. UserId: {UserId}, PurchaseId: {PurchaseId}, ItemCount: {ItemCount}, TotalAmount: {TotalAmount}",
            userId.Value,
            purchase.Id,
            dto.Items.Count,
            totalAmount);

        activity?.SetTag("purchase.exists", true);
        activity?.SetTag("purchase.id", purchase.Id);
        activity?.SetTag("purchase.item_count", dto.Items.Count);
        activity?.SetTag("purchase.total_amount", totalAmount);

        return Ok(dto);
    }

    [HttpPost("scan-product")]
    public async Task<ActionResult<PurchaseDto>> ScanProduct([FromBody] StartPurchaseDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("ScanProduct");
        activity?.SetTag("product.barcode", dto.ProductBarcode);
        
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to ScanProduct");
            return Unauthorized();
        }

        activity?.SetTag("user.id", userId.Value);
        _logger.LogInformation(
            "Product scan initiated. UserId: {UserId}, ProductBarcode: {ProductBarcode}",
            userId.Value,
            dto.ProductBarcode);

        // Find the product by barcode
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == dto.ProductBarcode);
        if (product == null)
        {
            _logger.LogWarning(
                "Product scan failed: Product not found. UserId: {UserId}, ProductBarcode: {ProductBarcode}",
                userId.Value,
                dto.ProductBarcode);
            
            activity?.SetStatus(ActivityStatusCode.Error, "Product not found");
            return BadRequest(new { message = "Product not found" });
        }

        activity?.SetTag("product.id", product.Id);
        activity?.SetTag("product.name", product.Name);
        activity?.SetTag("product.price", product.Price);

        _logger.LogInformation(
            "Product found. ProductId: {ProductId}, Name: {ProductName}, Price: {Price}",
            product.Id,
            product.Name,
            product.Price);

        // Get or create current purchase
        var purchase = await _context.Purchases
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CompletedAt == null);

        if (purchase == null)
        {
            purchase = new Purchase
            {
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "New purchase session started. PurchaseId: {PurchaseId}, UserId: {UserId}",
                purchase.Id,
                userId.Value);
            
            activity?.AddEvent(new ActivityEvent("purchase_started"));
        }
        else
        {
            _logger.LogDebug(
                "Using existing purchase session. PurchaseId: {PurchaseId}, UserId: {UserId}",
                purchase.Id,
                userId.Value);
        }

        activity?.SetTag("purchase.id", purchase.Id);

        // Check if user has an active payment barcode (we'll use the first active non-login barcode)
        var userBarcode = await _context.Barcodes
            .FirstOrDefaultAsync(b => b.UserId == userId.Value && b.IsActive && !b.IsLoginOnly);

        if (userBarcode == null)
        {
            _logger.LogError(
                "Product scan failed: No active payment barcode found. UserId: {UserId}, PurchaseId: {PurchaseId}",
                userId.Value,
                purchase.Id);
            
            activity?.SetStatus(ActivityStatusCode.Error, "No active payment barcode");
            return BadRequest(new { message = "No active payment barcode found for user" });
        }

        // Create barcode scan with product price
        var scan = new BarcodeScan
        {
            PurchaseId = purchase.Id,
            BarcodeId = userBarcode.Id,
            Amount = product.Price,
            ScannedAt = DateTime.UtcNow
        };

        _context.BarcodeScans.Add(scan);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product scanned successfully. ProductId: {ProductId}, ProductName: {ProductName}, Price: {Price}, " +
            "PurchaseId: {PurchaseId}, UserId: {UserId}",
            product.Id,
            product.Name,
            product.Price,
            purchase.Id,
            userId.Value);

        SnackboxTelemetry.ProductScanCounter.Add(1,
            new KeyValuePair<string, object?>("product.id", product.Id),
            new KeyValuePair<string, object?>("product.name", product.Name));

        // Reload to get updated scans
        await _context.Entry(purchase).Collection(p => p.Scans).LoadAsync();
        foreach (var s in purchase.Scans)
        {
            await _context.Entry(s).Reference(x => x.Barcode).LoadAsync();
        }

        var totalAmount = purchase.Scans.Sum(s => s.Amount);
        var resultDto = new PurchaseDto
        {
            Id = purchase.Id,
            UserId = purchase.UserId,
            Username = userBarcode.User.Username,
            TotalAmount = totalAmount,
            CreatedAt = purchase.CreatedAt,
            CompletedAt = purchase.CompletedAt,
            Items = purchase.Scans.Select(s => new PurchaseItemDto
            {
                Id = s.Id,
                ProductName = product.Name,
                Amount = s.Amount,
                ScannedAt = s.ScannedAt
            }).ToList()
        };

        _logger.LogDebug(
            "Purchase updated. PurchaseId: {PurchaseId}, TotalItems: {ItemCount}, TotalAmount: {TotalAmount}",
            purchase.Id,
            resultDto.Items.Count,
            totalAmount);

        activity?.SetTag("purchase.total_items", resultDto.Items.Count);
        activity?.SetTag("purchase.total_amount", totalAmount);

        return Ok(resultDto);
    }

    [HttpPost("complete")]
    public async Task<ActionResult<PurchaseDto>> CompletePurchase()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("CompletePurchase");
        
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to CompletePurchase");
            return Unauthorized();
        }

        activity?.SetTag("user.id", userId.Value);
        _logger.LogInformation("Attempting to complete purchase. UserId: {UserId}", userId.Value);

        var purchase = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CompletedAt == null);

        if (purchase == null)
        {
            _logger.LogWarning("Purchase completion failed: No active purchase found. UserId: {UserId}", userId.Value);
            activity?.SetStatus(ActivityStatusCode.Error, "No active purchase");
            return BadRequest(new { message = "No active purchase found" });
        }

        activity?.SetTag("purchase.id", purchase.Id);

        if (!purchase.Scans.Any())
        {
            _logger.LogWarning(
                "Purchase completion failed: No items in purchase. PurchaseId: {PurchaseId}, UserId: {UserId}",
                purchase.Id,
                userId.Value);
            
            activity?.SetStatus(ActivityStatusCode.Error, "No items in purchase");
            return BadRequest(new { message = "Cannot complete purchase with no items" });
        }

        var totalAmount = purchase.Scans.Sum(s => s.Amount);
        var itemCount = purchase.Scans.Count;

        purchase.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Purchase completed successfully. PurchaseId: {PurchaseId}, UserId: {UserId}, ItemCount: {ItemCount}, TotalAmount: {TotalAmount}",
            purchase.Id,
            userId.Value,
            itemCount,
            totalAmount);

        SnackboxTelemetry.PurchaseCounter.Add(1,
            new KeyValuePair<string, object?>("user.id", userId.Value));
        SnackboxTelemetry.PurchaseAmountHistogram.Record((double)totalAmount,
            new KeyValuePair<string, object?>("user.id", userId.Value));

        activity?.SetTag("purchase.item_count", itemCount);
        activity?.SetTag("purchase.total_amount", totalAmount);

        var dto = new PurchaseDto
        {
            Id = purchase.Id,
            UserId = purchase.UserId,
            Username = purchase.User.Username,
            TotalAmount = totalAmount,
            CreatedAt = purchase.CreatedAt,
            CompletedAt = purchase.CompletedAt,
            Items = purchase.Scans.Select(s => new PurchaseItemDto
            {
                Id = s.Id,
                ProductName = s.Barcode.Code,
                Amount = s.Amount,
                ScannedAt = s.ScannedAt
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost("cancel")]
    public async Task<ActionResult> CancelPurchase()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("CancelPurchase");
        
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to CancelPurchase");
            return Unauthorized();
        }

        activity?.SetTag("user.id", userId.Value);
        _logger.LogInformation("Attempting to cancel purchase. UserId: {UserId}", userId.Value);

        var purchase = await _context.Purchases
            .Include(p => p.Scans)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CompletedAt == null);

        if (purchase == null)
        {
            _logger.LogWarning("Purchase cancellation failed: No active purchase found. UserId: {UserId}", userId.Value);
            activity?.SetStatus(ActivityStatusCode.Error, "No active purchase");
            return BadRequest(new { message = "No active purchase found" });
        }

        activity?.SetTag("purchase.id", purchase.Id);
        var itemCount = purchase.Scans.Count;
        var totalAmount = purchase.Scans.Sum(s => s.Amount);

        _context.Purchases.Remove(purchase);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Purchase cancelled. PurchaseId: {PurchaseId}, UserId: {UserId}, ItemCount: {ItemCount}, TotalAmount: {TotalAmount}",
            purchase.Id,
            userId.Value,
            itemCount,
            totalAmount);

        activity?.SetTag("purchase.item_count", itemCount);
        activity?.SetTag("purchase.total_amount", totalAmount);

        return Ok(new { message = "Purchase cancelled" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetAll()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetAllPurchases");
        
        _logger.LogInformation("Admin fetching all completed purchases");

        var purchases = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .Where(p => p.CompletedAt != null)
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync();

        var dtos = purchases.Select(p => new PurchaseDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Username = p.User.Username,
            TotalAmount = p.Scans.Sum(s => s.Amount),
            CreatedAt = p.CreatedAt,
            CompletedAt = p.CompletedAt,
            Items = p.Scans.Select(s => new PurchaseItemDto
            {
                Id = s.Id,
                ProductName = s.Barcode.Code,
                Amount = s.Amount,
                ScannedAt = s.ScannedAt
            }).ToList()
        }).ToList();

        _logger.LogInformation(
            "Retrieved all completed purchases. Count: {PurchaseCount}, TotalAmount: {TotalAmount}",
            dtos.Count,
            dtos.Sum(p => p.TotalAmount));

        activity?.SetTag("purchases.count", dtos.Count);
        activity?.SetTag("purchases.total_amount", dtos.Sum(p => p.TotalAmount));

        return Ok(dtos);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return null;
        }
        return userId;
    }
}

