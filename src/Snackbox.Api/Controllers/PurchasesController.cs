using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Mappers;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PurchasesController> _logger;
    private readonly IConfiguration _configuration;
    private const int DefaultTimeoutSeconds = 60;

    public PurchasesController(ApplicationDbContext context, ILogger<PurchasesController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
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
            .Where(p => p.UserId == userId.Value)
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync();

        return Ok(purchases.ToDtoList());
    }

    [HttpGet("my-purchases/current")]
    public async Task<ActionResult<PurchaseDto?>> GetCurrentPurchase()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var timeoutSeconds = _configuration.GetValue("Scanner:TimeoutSeconds", DefaultTimeoutSeconds);
        var timeoutThreshold = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        var purchase = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.CompletedAt >= timeoutThreshold);

        if (purchase == null)
        {
            return Ok(null);
        }

        return Ok(purchase.ToDto());
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetAll()
    {
        var purchases = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync();

        return Ok(purchases.ToDtoList());
    }

    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetByUserId(int userId)
    {
        var purchases = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync();

        var dtos = purchases.Select(p => new PurchaseDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Username = p.User.Username,
            TotalAmount = p.ManualAmount ?? p.Scans.Sum(s => s.Amount),
            CreatedAt = p.CreatedAt,
            CompletedAt = p.CompletedAt,
            Type = p.Type.ToString(),
            ReferencePurchaseId = p.ReferencePurchaseId,
            ManualAmount = p.ManualAmount,
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

    [HttpPost("manual")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PurchaseDto>> CreateManual([FromBody] CreateManualPurchaseDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than zero" });
        }

        var createdAt = dto.CreatedAt ?? DateTime.UtcNow;
        var purchase = new Purchase
        {
            UserId = dto.UserId,
            CreatedAt = createdAt,
            CompletedAt = createdAt,
            Type = PurchaseType.Manual,
            ManualAmount = dto.Amount
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Manual purchase created: {PurchaseId} for user {UserId} - Amount: {Amount}",
            purchase.Id, purchase.UserId, purchase.ManualAmount);

        var resultDto = new PurchaseDto
        {
            Id = purchase.Id,
            UserId = purchase.UserId,
            Username = user.Username,
            TotalAmount = purchase.ManualAmount.Value,
            CreatedAt = purchase.CreatedAt,
            CompletedAt = purchase.CompletedAt,
            Type = purchase.Type.ToString(),
            ReferencePurchaseId = purchase.ReferencePurchaseId,
            ManualAmount = purchase.ManualAmount,
            Items = new List<PurchaseItemDto>()
        };

        return CreatedAtAction(nameof(GetByUserId), new { userId = purchase.UserId }, resultDto);
    }

    [HttpPost("correction")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PurchaseDto>> CreateCorrection([FromBody] CreatePurchaseCorrectionDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        var referencePurchase = await _context.Purchases.FindAsync(dto.ReferencePurchaseId);
        if (referencePurchase == null)
        {
            return BadRequest(new { message = "Reference purchase not found" });
        }

        if (referencePurchase.UserId != dto.UserId)
        {
            return BadRequest(new { message = "Reference purchase must belong to the same user" });
        }

        var purchase = new Purchase
        {
            UserId = dto.UserId,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Type = PurchaseType.Correction,
            ReferencePurchaseId = dto.ReferencePurchaseId,
            ManualAmount = dto.Amount
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Purchase correction created: {PurchaseId} for user {UserId} - Amount: {Amount}, Reference: {ReferencePurchaseId}",
            purchase.Id, purchase.UserId, purchase.ManualAmount, purchase.ReferencePurchaseId);

        var resultDto = new PurchaseDto
        {
            Id = purchase.Id,
            UserId = purchase.UserId,
            Username = user.Username,
            TotalAmount = purchase.ManualAmount.Value,
            CreatedAt = purchase.CreatedAt,
            CompletedAt = purchase.CompletedAt,
            Type = purchase.Type.ToString(),
            ReferencePurchaseId = purchase.ReferencePurchaseId,
            ManualAmount = purchase.ManualAmount,
            Items = new List<PurchaseItemDto>()
        };

        return CreatedAtAction(nameof(GetByUserId), new { userId = purchase.UserId }, resultDto);
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
