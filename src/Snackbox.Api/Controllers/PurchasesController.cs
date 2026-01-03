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

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetByUserId(int userId)
    {
        var purchases = await _context.Purchases
            .Include(p => p.User)
            .Include(p => p.Scans)
                .ThenInclude(s => s.Barcode)
            .Where(p => p.UserId == userId && p.CompletedAt != null)
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
