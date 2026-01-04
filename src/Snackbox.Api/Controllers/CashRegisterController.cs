using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CashRegisterController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CashRegisterController(ApplicationDbContext context)
    {
        _context = context;
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
}
