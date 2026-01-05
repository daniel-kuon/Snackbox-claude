using System.Security.Claims;
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
public class CashRegisterController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CashRegisterController> _logger;

    public CashRegisterController(ApplicationDbContext context, ILogger<CashRegisterController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<CashRegisterDto>> Get()
    {
        var cashRegister = await _context.CashRegister
            .Include(cr => cr.LastUpdatedByUser)
            .FirstOrDefaultAsync();

        if (cashRegister == null)
        {
            // Return default empty cash register
            return Ok(new CashRegisterDto
            {
                Id = 0,
                CurrentBalance = 0,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedByUserId = 0,
                LastUpdatedByUsername = "System"
            });
        }

        return Ok(new CashRegisterDto
        {
            Id = cashRegister.Id,
            CurrentBalance = cashRegister.CurrentBalance,
            LastUpdatedAt = cashRegister.LastUpdatedAt,
            LastUpdatedByUserId = cashRegister.LastUpdatedByUserId,
            LastUpdatedByUsername = cashRegister.LastUpdatedByUser.Username
        });
    }

    [HttpPost("correct")]
    public async Task<ActionResult<CashRegisterDto>> CorrectBalance([FromBody] CorrectCashRegisterDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        if (dto.NewBalance < 0)
        {
            return BadRequest(new { message = "Balance cannot be negative" });
        }

        var cashRegister = await _context.CashRegister.FirstOrDefaultAsync();
        
        if (cashRegister == null)
        {
            // Create new cash register with the corrected balance
            cashRegister = new CashRegister
            {
                CurrentBalance = dto.NewBalance,
                LastUpdatedAt = DateTime.UtcNow,
                LastUpdatedByUserId = userId
            };
            _context.CashRegister.Add(cashRegister);
        }
        else
        {
            var oldBalance = cashRegister.CurrentBalance;
            cashRegister.CurrentBalance = dto.NewBalance;
            cashRegister.LastUpdatedAt = DateTime.UtcNow;
            cashRegister.LastUpdatedByUserId = userId;
            
            _logger.LogInformation("Cash register balance corrected from {OldBalance} to {NewBalance} by user {UserId}",
                oldBalance, dto.NewBalance, userId);
        }

        await _context.SaveChangesAsync();

        // Fetch updated cash register with user info
        var updated = await _context.CashRegister
            .Include(cr => cr.LastUpdatedByUser)
            .FirstAsync();

        return Ok(new CashRegisterDto
        {
            Id = updated.Id,
            CurrentBalance = updated.CurrentBalance,
            LastUpdatedAt = updated.LastUpdatedAt,
            LastUpdatedByUserId = updated.LastUpdatedByUserId,
            LastUpdatedByUsername = updated.LastUpdatedByUser.Username
        });
    }
}
