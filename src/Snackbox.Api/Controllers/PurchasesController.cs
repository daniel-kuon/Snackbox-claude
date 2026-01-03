using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Mappers;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PurchasesController(ApplicationDbContext context)
    {
        _context = context;
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

        return Ok(purchases.ToDtoList());
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
            .Where(p => p.CompletedAt != null)
            .OrderByDescending(p => p.CompletedAt)
            .ToListAsync();

        return Ok(purchases.ToDtoList());
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
