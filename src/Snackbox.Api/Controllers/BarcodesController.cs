using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

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

        return Ok(barcodes);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<BarcodeDto>>> GetByUserId(int userId)
    {
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

        return Ok(barcodes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BarcodeDto>> GetById(int id)
    {
        var barcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (barcode == null)
        {
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

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<BarcodeDto>> Create([FromBody] CreateBarcodeDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        if (await _context.Barcodes.AnyAsync(b => b.Code == dto.Code))
        {
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

        _logger.LogInformation("Barcode created: {BarcodeId} - {Code} for user {UserId}", barcode.Id, barcode.Code, barcode.UserId);

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
        var barcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (barcode == null)
        {
            return NotFound(new { message = "Barcode not found" });
        }

        if (dto.Code != barcode.Code && await _context.Barcodes.AnyAsync(b => b.Code == dto.Code))
        {
            return BadRequest(new { message = "A barcode with this code already exists" });
        }

        barcode.Code = dto.Code;
        barcode.Amount = dto.Amount;
        barcode.IsActive = dto.IsActive;
        barcode.IsLoginOnly = dto.IsLoginOnly;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Barcode updated: {BarcodeId} - {Code}", barcode.Id, barcode.Code);

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
        var barcode = await _context.Barcodes
            .Include(b => b.Scans)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (barcode == null)
        {
            return NotFound(new { message = "Barcode not found" });
        }

        if (barcode.Scans.Any())
        {
            return BadRequest(new { message = "Cannot delete barcode with existing scans. Deactivate it instead." });
        }

        _context.Barcodes.Remove(barcode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Barcode deleted: {BarcodeId} - {Code}", barcode.Id, barcode.Code);

        return NoContent();
    }
}
