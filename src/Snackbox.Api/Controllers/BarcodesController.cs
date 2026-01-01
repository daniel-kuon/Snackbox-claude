using System.Diagnostics;
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
[Authorize(Roles = "Admin")]
public class BarcodesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BarcodesController> _logger;

    public BarcodesController(ApplicationDbContext context, ILogger<BarcodesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BarcodeDto>>> GetAll()
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetAllBarcodes");
        _logger.LogInformation("Fetching all barcodes");
        
        var barcodes = await _context.Barcodes
            .Include(b => b.User)
            .Select(b => new BarcodeDto
            {
                Id = b.Id,
                UserId = b.UserId,
                Username = b.User.Username,
                Code = b.Code,
                Amount = b.Amount,
                IsActive = b.IsActive,
                IsLoginOnly = b.IsLoginOnly,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {BarcodeCount} barcodes", barcodes.Count);
        activity?.SetTag("barcodes.count", barcodes.Count);

        return Ok(barcodes);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<BarcodeDto>>> GetByUserId(int userId)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetBarcodesByUserId");
        activity?.SetTag("user.id", userId);
        _logger.LogInformation("Fetching barcodes for user. UserId: {UserId}", userId);
        
        var barcodes = await _context.Barcodes
            .Include(b => b.User)
            .Where(b => b.UserId == userId)
            .Select(b => new BarcodeDto
            {
                Id = b.Id,
                UserId = b.UserId,
                Username = b.User.Username,
                Code = b.Code,
                Amount = b.Amount,
                IsActive = b.IsActive,
                IsLoginOnly = b.IsLoginOnly,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {BarcodeCount} barcodes for user. UserId: {UserId}", barcodes.Count, userId);
        activity?.SetTag("barcodes.count", barcodes.Count);

        return Ok(barcodes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BarcodeDto>> GetById(int id)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GetBarcodeById");
        activity?.SetTag("barcode.id", id);
        _logger.LogInformation("Fetching barcode by ID. BarcodeId: {BarcodeId}", id);
        
        var barcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (barcode == null)
        {
            _logger.LogWarning("Barcode not found. BarcodeId: {BarcodeId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Barcode not found");
            return NotFound(new { message = "Barcode not found" });
        }

        var dto = new BarcodeDto
        {
            Id = barcode.Id,
            UserId = barcode.UserId,
            Username = barcode.User.Username,
            Code = barcode.Code,
            Amount = barcode.Amount,
            IsActive = barcode.IsActive,
            IsLoginOnly = barcode.IsLoginOnly,
            CreatedAt = barcode.CreatedAt
        };

        _logger.LogInformation("Barcode retrieved. BarcodeId: {BarcodeId}, UserId: {UserId}, IsActive: {IsActive}", 
            id, barcode.UserId, barcode.IsActive);

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<BarcodeDto>> Create([FromBody] CreateBarcodeDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("CreateBarcode");
        activity?.SetTag("user.id", dto.UserId);
        activity?.SetTag("barcode.is_login_only", dto.IsLoginOnly);
        
        _logger.LogInformation("Creating barcode. UserId: {UserId}, IsLoginOnly: {IsLoginOnly}, Amount: {Amount}", 
            dto.UserId, dto.IsLoginOnly, dto.Amount);

        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            _logger.LogWarning("Barcode creation failed: User not found. UserId: {UserId}", dto.UserId);
            activity?.SetStatus(ActivityStatusCode.Error, "User not found");
            return BadRequest(new { message = "User not found" });
        }

        if (await _context.Barcodes.AnyAsync(b => b.Code == dto.Code))
        {
            _logger.LogWarning("Barcode creation failed: Code already exists. Code: {Code}", dto.Code);
            activity?.SetStatus(ActivityStatusCode.Error, "Code already exists");
            return BadRequest(new { message = "A barcode with this code already exists" });
        }

        var barcode = new Barcode
        {
            UserId = dto.UserId,
            Code = dto.Code,
            Amount = dto.Amount,
            IsActive = dto.IsActive,
            IsLoginOnly = dto.IsLoginOnly,
            CreatedAt = DateTime.UtcNow
        };

        _context.Barcodes.Add(barcode);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Barcode created successfully. BarcodeId: {BarcodeId}, Code: {Code}, UserId: {UserId}, IsActive: {IsActive}, IsLoginOnly: {IsLoginOnly}",
            barcode.Id, barcode.Code, barcode.UserId, barcode.IsActive, barcode.IsLoginOnly);
        
        activity?.SetTag("barcode.id", barcode.Id);

        var resultDto = new BarcodeDto
        {
            Id = barcode.Id,
            UserId = barcode.UserId,
            Username = user.Username,
            Code = barcode.Code,
            Amount = barcode.Amount,
            IsActive = barcode.IsActive,
            IsLoginOnly = barcode.IsLoginOnly,
            CreatedAt = barcode.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = barcode.Id }, resultDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BarcodeDto>> Update(int id, [FromBody] UpdateBarcodeDto dto)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("UpdateBarcode");
        activity?.SetTag("barcode.id", id);
        
        _logger.LogInformation("Updating barcode. BarcodeId: {BarcodeId}", id);

        var barcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (barcode == null)
        {
            _logger.LogWarning("Barcode update failed: Barcode not found. BarcodeId: {BarcodeId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Barcode not found");
            return NotFound(new { message = "Barcode not found" });
        }

        if (dto.Code != barcode.Code && await _context.Barcodes.AnyAsync(b => b.Code == dto.Code))
        {
            _logger.LogWarning("Barcode update failed: New code already exists. BarcodeId: {BarcodeId}, NewCode: {NewCode}", id, dto.Code);
            activity?.SetStatus(ActivityStatusCode.Error, "Code already exists");
            return BadRequest(new { message = "A barcode with this code already exists" });
        }

        barcode.Code = dto.Code;
        barcode.Amount = dto.Amount;
        barcode.IsActive = dto.IsActive;
        barcode.IsLoginOnly = dto.IsLoginOnly;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Barcode updated successfully. BarcodeId: {BarcodeId}, Code: {Code}, IsActive: {IsActive}, IsLoginOnly: {IsLoginOnly}",
            barcode.Id, barcode.Code, barcode.IsActive, barcode.IsLoginOnly);

        var resultDto = new BarcodeDto
        {
            Id = barcode.Id,
            UserId = barcode.UserId,
            Username = barcode.User.Username,
            Code = barcode.Code,
            Amount = barcode.Amount,
            IsActive = barcode.IsActive,
            IsLoginOnly = barcode.IsLoginOnly,
            CreatedAt = barcode.CreatedAt
        };

        return Ok(resultDto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("DeleteBarcode");
        activity?.SetTag("barcode.id", id);
        
        _logger.LogInformation("Attempting to delete barcode. BarcodeId: {BarcodeId}", id);

        var barcode = await _context.Barcodes
            .Include(b => b.Scans)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (barcode == null)
        {
            _logger.LogWarning("Barcode deletion failed: Barcode not found. BarcodeId: {BarcodeId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Barcode not found");
            return NotFound(new { message = "Barcode not found" });
        }

        if (barcode.Scans.Any())
        {
            _logger.LogWarning(
                "Barcode deletion failed: Barcode has {ScanCount} existing scans. BarcodeId: {BarcodeId}",
                barcode.Scans.Count, id);
            activity?.SetStatus(ActivityStatusCode.Error, "Barcode has existing scans");
            return BadRequest(new { message = "Cannot delete barcode with existing scans. Deactivate it instead." });
        }

        _context.Barcodes.Remove(barcode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Barcode deleted successfully. BarcodeId: {BarcodeId}, Code: {Code}", barcode.Id, barcode.Code);

        return NoContent();
    }
}
