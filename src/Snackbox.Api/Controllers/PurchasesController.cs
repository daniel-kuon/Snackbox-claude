using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Models;

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
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

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

        return Ok(dtos);
    }

    [HttpGet("my-purchases/current")]
    public async Task<ActionResult<PurchaseDto?>> GetCurrentPurchase()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var purchase = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CompletedAt == null);

        if (purchase == null)
        {
            return Ok(null);
        }

        var dto = new PurchaseDto
        {
            Id = purchase.Id,
            UserId = purchase.UserId,
            Username = purchase.User.Username,
            TotalAmount = purchase.Scans.Sum(s => s.Amount),
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

    [HttpPost("scan-product")]
    public async Task<ActionResult<PurchaseDto>> ScanProduct([FromBody] StartPurchaseDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        // Find the product by barcode
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == dto.ProductBarcode);
        if (product == null)
        {
            return BadRequest(new { message = "Product not found" });
        }

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

            _logger.LogInformation("New purchase started: {PurchaseId} for user {UserId}", purchase.Id, userId.Value);
        }

        // Check if user has an active payment barcode (we'll use the first active non-login barcode)
        var userBarcode = await _context.Barcodes
            .FirstOrDefaultAsync(b => b.UserId == userId.Value && b.IsActive && !b.IsLoginOnly);

        if (userBarcode == null)
        {
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

        _logger.LogInformation("Product scanned: {ProductId} - {ProductName} for purchase {PurchaseId}",
            product.Id, product.Name, purchase.Id);

        // Reload to get updated scans
        await _context.Entry(purchase).Collection(p => p.Scans).LoadAsync();
        foreach (var s in purchase.Scans)
        {
            await _context.Entry(s).Reference(x => x.Barcode).LoadAsync();
        }

        var resultDto = new PurchaseDto
        {
            Id = purchase.Id,
            UserId = purchase.UserId,
            Username = userBarcode.User.Username,
            TotalAmount = purchase.Scans.Sum(s => s.Amount),
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

        return Ok(resultDto);
    }

    [HttpPost("complete")]
    public async Task<ActionResult<PurchaseDto>> CompletePurchase()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var purchase = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CompletedAt == null);

        if (purchase == null)
        {
            return BadRequest(new { message = "No active purchase found" });
        }

        if (!purchase.Scans.Any())
        {
            return BadRequest(new { message = "Cannot complete purchase with no items" });
        }

        purchase.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Purchase completed: {PurchaseId} for user {UserId} - Total: {Amount}",
            purchase.Id, userId.Value, purchase.Scans.Sum(s => s.Amount));

        var dto = new PurchaseDto
        {
            Id = purchase.Id,
            UserId = purchase.UserId,
            Username = purchase.User.Username,
            TotalAmount = purchase.Scans.Sum(s => s.Amount),
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
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var purchase = await _context.Purchases
            .Include(p => p.Scans)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CompletedAt == null);

        if (purchase == null)
        {
            return BadRequest(new { message = "No active purchase found" });
        }

        _context.Purchases.Remove(purchase);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Purchase cancelled: {PurchaseId} for user {UserId}", purchase.Id, userId.Value);

        return Ok(new { message = "Purchase cancelled" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetAll()
    {
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
