using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.Dtos;
using Snackbox.Api.Services;
using System.Security.Claims;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        LoginResponse? response;

        // Try barcode and password authentication if both are provided
        if (!string.IsNullOrWhiteSpace(request.BarcodeValue) && !string.IsNullOrWhiteSpace(request.Password))
        {
            response = await _authenticationService.AuthenticateWithBarcodeAndPasswordAsync(request.BarcodeValue, request.Password);
            if (response != null)
            {
                _logger.LogInformation("User {Username} logged in with barcode and password", response.Username);
                return Ok(response);
            }

            _logger.LogWarning("Failed barcode and password login attempt");
            return Unauthorized(new { message = "Invalid barcode or password" });
        }

        // Try password authentication if username and password are provided
        if (!string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
        {
            response = await _authenticationService.AuthenticateWithPasswordAsync(request.Username, request.Password);
            if (response != null)
            {
                _logger.LogInformation("User {Username} logged in with password", response.Username);
                return Ok(response);
            }
        }

        // Try barcode authentication if barcode is provided
        if (!string.IsNullOrWhiteSpace(request.BarcodeValue))
        {
            response = await _authenticationService.AuthenticateAsync(request.BarcodeValue);
            if (response != null)
            {
                _logger.LogInformation("User {Username} logged in with barcode", response.Username);
                return Ok(response);
            }
        }

        _logger.LogWarning("Failed login attempt");
        return Unauthorized(new { message = "Invalid credentials" });
    }

    [HttpPost("set-password")]
    public async Task<ActionResult> SetPassword([FromBody] SetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BarcodeValue) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Barcode, email, and password are required" });
        }

        try
        {
            var success = await _authenticationService.SetPasswordAsync(request.BarcodeValue, request.Email, request.NewPassword);

            if (!success)
            {
                return BadRequest(new { message = "Invalid barcode, email doesn't match, or unable to set password" });
            }

            _logger.LogInformation("Password set/reset successfully for barcode");
            return Ok(new { message = "Password set successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Attempt to set password failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("has-password/{username}")]
    public async Task<ActionResult<bool>> HasPassword(string username)
    {
        var hasPassword = await _authenticationService.UserHasPasswordAsync(username);
        return Ok(new { hasPassword });
    }

    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Current and new password are required" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        var success = await _authenticationService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { message = "Invalid current password or user does not have a password set" });
        }

        _logger.LogInformation("User {UserId} changed password successfully", userId);
        return Ok(new { message = "Password changed successfully" });
    }
}
