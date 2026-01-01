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
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _context.Users
            .Include(u => u.Purchases)
                .ThenInclude(p => p.Scans)
            .Include(u => u.Payments)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                IsAdmin = u.IsAdmin,
                Balance = u.Payments.Sum(p => p.Amount) - u.Purchases.SelectMany(p => p.Scans).Sum(s => s.Amount),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _context.Users
            .Include(u => u.Purchases)
                .ThenInclude(p => p.Scans)
            .Include(u => u.Payments)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var dto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            Balance = user.Payments.Sum(p => p.Amount) - user.Purchases.SelectMany(p => p.Scans).Sum(s => s.Amount),
            CreatedAt = user.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        // In production, use proper password hashing (BCrypt, Argon2, etc.)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            IsAdmin = dto.IsAdmin,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {UserId} - {Username}", user.Id, user.Username);

        var resultDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            Balance = 0,
            CreatedAt = user.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, resultDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Purchases)
                .ThenInclude(p => p.Scans)
            .Include(u => u.Payments)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (dto.Username != user.Username && await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        if (dto.Email != user.Email && await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        user.Username = dto.Username;
        user.Email = dto.Email;
        user.IsAdmin = dto.IsAdmin;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId} - {Username}", user.Id, user.Username);

        var resultDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            Balance = user.Payments.Sum(p => p.Amount) - user.Purchases.SelectMany(p => p.Scans).Sum(s => s.Amount),
            CreatedAt = user.CreatedAt
        };

        return Ok(resultDto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var user = await _context.Users
            .Include(u => u.Barcodes)
            .Include(u => u.Purchases)
            .Include(u => u.Payments)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (user.Barcodes.Any() || user.Purchases.Any() || user.Payments.Any())
        {
            return BadRequest(new { message = "Cannot delete user with existing barcodes, purchases, or payments" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted: {UserId} - {Username}", user.Id, user.Username);

        return NoContent();
    }
}
