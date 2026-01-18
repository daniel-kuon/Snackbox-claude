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
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll([FromQuery] bool includeRetired = false)
    {
        var usersQuery = _context.Users
            .Include(u => u.Purchases)
                .ThenInclude(p => p.Scans)
            .Include(u => u.Payments)
            .Include(u => u.Withdrawals)
            .Select(u => new
            {
                User = u,
                Balance = u.Payments.Sum(p => p.Amount) - u.Purchases.Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount)) - u.Withdrawals.Sum(w => w.Amount)
            });

        // Hide retired users by default unless they have a non-zero balance
        if (!includeRetired)
        {
            usersQuery = usersQuery.Where(x => !x.User.IsRetired || x.Balance != 0);
        }

        var users = await usersQuery
            .Select(x => new UserDto
            {
                Id = x.User.Id,
                Username = x.User.Username,
                Email = x.User.Email,
                IsAdmin = x.User.IsAdmin,
                IsActive = x.User.IsActive,
                IsRetired = x.User.IsRetired,
                Balance = x.Balance,
                CreatedAt = x.User.CreatedAt,
                HasPurchases = x.User.Purchases.Any()
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
            .Include(u => u.Withdrawals)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var balance = user.Payments.Sum(p => p.Amount) - user.Purchases.Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount)) - user.Withdrawals.Sum(w => w.Amount);
        return Ok(user.ToDtoWithBalance(balance));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterUserDto dto)
    {
        // Check if barcode already exists
        var existingBarcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Code == dto.BarcodeValue);

        if (existingBarcode != null && existingBarcode.UserId != 0)
        {
            return BadRequest(new { message = "This barcode is already registered to another user." });
        }

        // Check if email is already used (if provided)
        if (!string.IsNullOrWhiteSpace(dto.Email) && await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        // Check if this is the first user - make them admin
        var isFirstUser = !await _context.Users.AnyAsync();

        // Create user
        var user = new User
        {
            Username = dto.Name,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email,
            PasswordHash = string.IsNullOrWhiteSpace(dto.Password) ? null : BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsAdmin = isFirstUser, // First user becomes admin
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create and associate login barcode with user
        var barcode = new LoginBarcode
        {
            Code = dto.BarcodeValue,
            UserId = user.Id,
            Amount = 0m,
            CreatedAt = DateTime.UtcNow
        };

        _context.Barcodes.Add(barcode);
        await _context.SaveChangesAsync();

        // Verify the barcode was actually saved
        var verifyBarcode = await _context.Barcodes
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code == dto.BarcodeValue);

        if (verifyBarcode == null)
        {
            _logger.LogError("Barcode verification failed after save! Code: {BarcodeCode}", dto.BarcodeValue);
        }
        else
        {
            _logger.LogInformation("Barcode verified in database: {BarcodeId}, Code: {BarcodeCode}", verifyBarcode.Id, verifyBarcode.Code);
        }

        _logger.LogInformation("User registered: {UserId} - {Username} with new barcode {BarcodeId} (IsAdmin: {IsAdmin})",
            user.Id, user.Username, barcode.Id, user.IsAdmin);

        return Ok(new { message = "Registration successful", userId = user.Id, isAdmin = user.IsAdmin });
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

        // Check if purchase barcodes already exist
        if (!string.IsNullOrWhiteSpace(dto.PurchaseBarcode1) && await _context.Barcodes.AnyAsync(b => b.Code == dto.PurchaseBarcode1))
        {
            return BadRequest(new { message = "Purchase barcode 1 already exists" });
        }

        if (!string.IsNullOrWhiteSpace(dto.PurchaseBarcode2) && await _context.Barcodes.AnyAsync(b => b.Code == dto.PurchaseBarcode2))
        {
            return BadRequest(new { message = "Purchase barcode 2 already exists" });
        }

        // In production, use proper password hashing (BCrypt, Argon2, etc.)
        var passwordHash = !string.IsNullOrWhiteSpace(dto.Password) ? BCrypt.Net.BCrypt.HashPassword(dto.Password) : null;

        var user = dto.ToEntity();
        user.PasswordHash = passwordHash;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create purchase barcodes if provided
        if (!string.IsNullOrWhiteSpace(dto.PurchaseBarcode1))
        {
            var barcode1 = new PurchaseBarcode
            {
                Code = dto.PurchaseBarcode1,
                UserId = user.Id,
                Amount = 0.50m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Barcodes.Add(barcode1);
        }

        if (!string.IsNullOrWhiteSpace(dto.PurchaseBarcode2))
        {
            var barcode2 = new PurchaseBarcode
            {
                Code = dto.PurchaseBarcode2,
                UserId = user.Id,
                Amount = 0.30m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Barcodes.Add(barcode2);
        }

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
            .Include(u => u.Withdrawals)
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

        var balance = user.Payments.Sum(p => p.Amount) - user.Purchases.Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount)) - user.Withdrawals.Sum(w => w.Amount);
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

    /// <summary>
    /// Retire a user: mark as retired and inactive, create an inactive placeholder user ("User {count}")
    /// and move all barcodes from the retired user to the placeholder.
    /// </summary>
    [HttpPost("{id}/retire")]
    public async Task<ActionResult> RetireUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Barcodes)
            .Include(u => u.Purchases)
                .ThenInclude(p => p.Scans)
            .Include(u => u.Payments)
            .Include(u => u.Withdrawals)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (user.IsRetired)
        {
            return BadRequest(new { message = "User is already retired" });
        }

        // Mark user as retired and inactive
        user.IsActive = false;
        user.IsRetired = true;

        // Create placeholder inactive user
        var userCount = await _context.Users.CountAsync();
        var placeholder = new User
        {
            Username = $"User {userCount + 1}",
            Email = null,
            PasswordHash = null,
            IsAdmin = false,
            IsActive = false,
            IsRetired = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(placeholder);
        await _context.SaveChangesAsync();

        // Move barcodes to placeholder
        foreach (var bc in user.Barcodes)
        {
            bc.UserId = placeholder.Id;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User retired: {UserId} - {Username}. Moved {BarcodeCount} barcodes to placeholder {PlaceholderId}",
            user.Id, user.Username, user.Barcodes.Count, placeholder.Id);

        // Return the retired user and the new placeholder info
        var balance = user.Payments.Sum(p => p.Amount) - user.Purchases.Sum(p => p.ManualAmount ?? p.Scans.Sum(s => s.Amount)) - user.Withdrawals.Sum(w => w.Amount);
        return Ok(new
        {
            RetiredUser = user.ToDtoWithBalance(balance),
            PlaceholderUserId = placeholder.Id,
            PlaceholderUsername = placeholder.Username
        });
    }

    [HttpGet("{id}/achievements")]
    public async Task<ActionResult<IEnumerable<AchievementDto>>> GetUserAchievements(int id)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
        {
            return NotFound(new { message = "User not found" });
        }

        var achievements = await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == id)
            .OrderByDescending(ua => ua.EarnedAt)
            .Select(ua => new AchievementDto
            {
                Id = ua.Achievement.Id,
                Code = ua.Achievement.Code,
                Name = ua.Achievement.Name,
                Description = ua.Achievement.Description,
                Category = ua.Achievement.Category.ToString(),
                ImageUrl = ua.Achievement.ImageUrl,
                EarnedAt = ua.EarnedAt
            })
            .ToListAsync();

        return Ok(achievements);
    }
}
