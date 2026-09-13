using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;

namespace Snackbox.Api.Controllers;

/// <summary>
/// Development-only helper for UI testing: lists all users with their barcodes so the
/// scan screen can simulate scans by click, and issues tokens without a password so the
/// login screen can be bypassed. Every endpoint returns 404 unless the app runs in the
/// Development environment AND TestHelper:Enabled is true.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestHelperController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly IAuthenticationService _authenticationService;
    private readonly DatabaseSeeder _seeder;
    private readonly ILogger<TestHelperController> _logger;

    public TestHelperController(ApplicationDbContext context,
                                IConfiguration configuration,
                                IHostEnvironment environment,
                                IAuthenticationService authenticationService,
                                DatabaseSeeder seeder,
                                ILogger<TestHelperController> logger)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
        _authenticationService = authenticationService;
        _seeder = seeder;
        _logger = logger;
    }

    private bool IsEnabled =>
        _environment.IsDevelopment() && _configuration.GetValue<bool>("TestHelper:Enabled");

    [HttpGet("users")]
    public async Task<ActionResult<List<TestUserDto>>> GetUsers()
    {
        if (!IsEnabled)
            return NotFound();

        var users = await _context.Users
            .Include(u => u.Barcodes)
            .Where(u => !u.IsRetired)
            .OrderBy(u => u.Username)
            .Select(u => new TestUserDto
            {
                UserId = u.Id,
                Username = u.Username,
                IsAdmin = u.IsAdmin,
                IsActive = u.IsActive,
                Barcodes = u.Barcodes
                    .Select(b => new TestBarcodeDto
                    {
                        Code = b.Code,
                        Amount = b.Amount,
                        IsLoginBarcode = b is LoginBarcode
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] TestLoginRequest request)
    {
        if (!IsEnabled)
            return NotFound();

        var response = await _authenticationService.AuthenticateAsUserAsync(request.UserId);
        if (response == null)
            return NotFound(new { message = "User not found" });

        _logger.LogWarning("TEST HELPER: password bypass login as user {UserId} ({Username})",
            response.UserId, response.Username);

        return Ok(response);
    }

    [HttpPost("reset-database")]
    public async Task<ActionResult> ResetDatabase()
    {
        if (!IsEnabled)
            return NotFound();

        _logger.LogWarning("TEST HELPER: resetting database (drop, migrate, reseed)");

        _context.ChangeTracker.Clear();
        await _context.Database.EnsureDeletedAsync();
        if (_context.Database.IsRelational())
        {
            await _context.Database.MigrateAsync();
        }
        else
        {
            // In-memory provider (tests) has no migrations
            await _context.Database.EnsureCreatedAsync();
        }
        await _seeder.SeedSampleDataAsync();

        return Ok(new { message = "Database reset and reseeded" });
    }
}
