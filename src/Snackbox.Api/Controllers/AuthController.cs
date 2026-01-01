using Microsoft.AspNetCore.Mvc;
using Snackbox.Api.DTOs;
using Snackbox.Api.Services;

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
        LoginResponse? response = null;

        // Try password authentication first if username and password are provided
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

        var success = await _authenticationService.SetPasswordAsync(request.BarcodeValue, request.Email, request.NewPassword);

        if (!success)
        {
            return BadRequest(new { message = "Invalid barcode, email doesn't match, or unable to set password" });
        }

        _logger.LogInformation("Password set successfully for barcode");
        return Ok(new { message = "Password set successfully" });
    }

    [HttpGet("has-password/{username}")]
    public async Task<ActionResult<bool>> HasPassword(string username)
    {
        var hasPassword = await _authenticationService.UserHasPasswordAsync(username);
        return Ok(new { hasPassword });
    }
}
