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
        if (string.IsNullOrWhiteSpace(request.BarcodeValue))
        {
            return BadRequest(new { message = "Barcode value is required" });
        }

        var response = await _authenticationService.AuthenticateAsync(request.BarcodeValue);

        if (response == null)
        {
            _logger.LogWarning("Failed login attempt with barcode: {BarcodeValue}", request.BarcodeValue);
            return Unauthorized(new { message = "Invalid barcode or barcode is not active" });
        }

        _logger.LogInformation("User {Username} logged in successfully", response.Username);
        return Ok(response);
    }
}
