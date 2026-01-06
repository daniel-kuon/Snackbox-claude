using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class WithdrawalsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WithdrawalsController> _logger;

    public WithdrawalsController(ApplicationDbContext context, ILogger<WithdrawalsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WithdrawalDto>>> GetAll()
    {
        var withdrawals = await _context.Withdrawals
            .Include(w => w.User)
            .OrderByDescending(w => w.WithdrawnAt)
            .ToListAsync();

        return Ok(withdrawals.ToDtoListWithUser());
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<WithdrawalDto>>> GetByUserId(int userId)
    {
        var withdrawals = await _context.Withdrawals
            .Include(w => w.User)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.WithdrawnAt)
            .ToListAsync();

        return Ok(withdrawals.ToDtoListWithUser());
    }

    [HttpPost]
    public async Task<ActionResult<WithdrawalDto>> Create([FromBody] CreateWithdrawalDto dto)
    {
        // Get current user ID from claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        if (!user.IsAdmin)
        {
            return BadRequest(new { message = "User must be an admin to make withdrawals" });
        }

        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Withdrawal amount must be greater than zero" });
        }

        // Check cash register has sufficient balance
        var cashRegister = await _context.CashRegister.FirstOrDefaultAsync();
        if (cashRegister == null || cashRegister.CurrentBalance < dto.Amount)
        {
            return BadRequest(new { message = "Insufficient cash in register" });
        }

        var withdrawal = new Withdrawal
        {
            UserId = userId,
            Amount = dto.Amount,
            Notes = dto.Notes,
            WithdrawnAt = DateTime.UtcNow
        };
        _context.Withdrawals.Add(withdrawal);

        // Update cash register balance
        cashRegister.CurrentBalance -= dto.Amount;
        cashRegister.LastUpdatedAt = DateTime.UtcNow;
        cashRegister.LastUpdatedByUserId = user.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Withdrawal created: {WithdrawalId} for user {UserId} - Amount: {Amount}",
            withdrawal.Id, withdrawal.UserId, withdrawal.Amount);

        withdrawal.User = user;
        var resultDto = withdrawal.ToDtoWithUser();

        return CreatedAtAction(nameof(GetByUserId), new { userId = withdrawal.UserId }, resultDto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var withdrawal = await _context.Withdrawals.FindAsync(id);

        if (withdrawal == null)
        {
            return NotFound(new { message = "Withdrawal not found" });
        }

        // Withdrawals cannot be deleted - only corrections can be made
        return BadRequest(new { message = "Withdrawals cannot be deleted. Please create a payment correction if needed." });
    }
}
