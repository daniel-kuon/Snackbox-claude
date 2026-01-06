using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DepositsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DepositsController> _logger;

    public DepositsController(ApplicationDbContext context, ILogger<DepositsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepositDto>>> GetAll()
    {
        var deposits = await _context.Deposits
            .Include(d => d.User)
            .OrderByDescending(d => d.DepositedAt)
            .ToListAsync();

        return Ok(deposits.ToDtoList());
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<DepositDto>>> GetByUserId(int userId)
    {
        var deposits = await _context.Deposits
            .Include(d => d.User)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DepositedAt)
            .ToListAsync();

        return Ok(deposits.ToDtoList());
    }
}
