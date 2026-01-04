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
                Balance = u.Payments.Sum(p => p.Amount) - u.Purchases.Where(p => p.CompletedAt.HasValue).Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount)),
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

        var balance = user.Payments.Sum(p => p.Amount) - user.Purchases.Where(p => p.CompletedAt.HasValue).Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount));
        return Ok(user.ToDtoWithBalance(balance));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterUserDto dto)
    {
        // Check if barcode exists
        var barcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Code == dto.BarcodeValue);

        if (barcode == null)
        {
            return BadRequest(new { message = "Invalid barcode. This barcode does not exist in the system." });
        }

        // Check if email is already used (if provided)
        if (!string.IsNullOrWhiteSpace(dto.Email) && await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        // Create user
        var user = new User
        {
            Username = dto.Name,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email,
            PasswordHash = string.IsNullOrWhiteSpace(dto.Password) ? null : BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Associate barcode with user
        barcode.UserId = user.Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered: {UserId} - {Username} via barcode {BarcodeId}", user.Id, user.Username, barcode.Id);

        return Ok(new { message = "Registration successful", userId = user.Id });
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        // In production, use proper password hashing (BCrypt, Argon2, etc.)
        var passwordHash = !string.IsNullOrWhiteSpace(dto.Password) ? BCrypt.Net.BCrypt.HashPassword(dto.Password) : null;

        var user = dto.ToEntity();
        user.PasswordHash = passwordHash;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {UserId} - {Username}", user.Id, user.Username);

        var resultDto = user.ToDtoWithBalance(0);

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

        user.UpdateFromDto(dto);

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId} - {Username}", user.Id, user.Username);

        var balance = user.Payments.Sum(p => p.Amount) - user.Purchases.Where(p => p.CompletedAt.HasValue).Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount));
        return Ok(user.ToDtoWithBalance(balance));
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
