using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using Snackbox.Api.Data;
using Snackbox.Api.DTOs;
using Snackbox.Api.Telemetry;

namespace Snackbox.Api.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        ApplicationDbContext context, 
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse?> AuthenticateAsync(string barcodeValue)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("AuthenticateAsync");
        activity?.SetTag("barcode.masked", MaskBarcode(barcodeValue));
        
        SnackboxTelemetry.AuthenticationAttemptCounter.Add(1);
        
        _logger.LogInformation("Authentication attempt started with barcode: {BarcodeMasked}", 
            MaskBarcode(barcodeValue));

        try
        {
            // Find the barcode with the user
            var barcode = await _context.Barcodes
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Code == barcodeValue && b.IsActive);

            if (barcode == null)
            {
                _logger.LogWarning("Authentication failed: Barcode not found or inactive. Barcode: {BarcodeMasked}", 
                    MaskBarcode(barcodeValue));
                
                SnackboxTelemetry.AuthenticationFailureCounter.Add(1, 
                    new KeyValuePair<string, object?>("reason", "barcode_not_found"));
                
                activity?.SetStatus(ActivityStatusCode.Error, "Barcode not found");
                activity?.SetTag("auth.success", false);
                activity?.SetTag("auth.failure_reason", "barcode_not_found");
                
                return null;
            }

            activity?.SetTag("user.id", barcode.User.Id);
            activity?.SetTag("user.username", barcode.User.Username);
            activity?.SetTag("user.is_admin", barcode.User.IsAdmin);

            _logger.LogInformation(
                "Barcode found for user. UserId: {UserId}, Username: {Username}, IsAdmin: {IsAdmin}",
                barcode.User.Id,
                barcode.User.Username,
                barcode.User.IsAdmin);

            // Generate JWT token
            var token = GenerateJwtToken(barcode.User);

            var response = new LoginResponse
            {
                Token = token,
                Username = barcode.User.Username,
                Email = barcode.User.Email,
                IsAdmin = barcode.User.IsAdmin,
                UserId = barcode.User.Id
            };

            _logger.LogInformation(
                "Authentication successful. UserId: {UserId}, Username: {Username}, IsAdmin: {IsAdmin}, TokenLength: {TokenLength}",
                barcode.User.Id,
                barcode.User.Username,
                barcode.User.IsAdmin,
                token.Length);

            SnackboxTelemetry.AuthenticationSuccessCounter.Add(1,
                new KeyValuePair<string, object?>("user.is_admin", barcode.User.IsAdmin));

            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("auth.success", true);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Authentication failed with exception. Barcode: {BarcodeMasked}", 
                MaskBarcode(barcodeValue));
            
            SnackboxTelemetry.AuthenticationFailureCounter.Add(1,
                new KeyValuePair<string, object?>("reason", "exception"));
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            
            throw;
        }
    }

    public async Task<LoginResponse?> AuthenticateWithPasswordAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        // Verify password with BCrypt
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            UserId = user.Id
        };
    }

    public async Task<bool> SetPasswordAsync(string barcodeValue, string email, string newPassword)
    {
        // Verify barcode exists and get associated user
        var barcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Code == barcodeValue && b.IsActive);

        if (barcode == null)
        {
            return false;
        }

        // Verify email matches the user's email
        if (!string.Equals(barcode.User.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Hash and set the password
        barcode.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UserHasPasswordAsync(string username)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        return user != null && !string.IsNullOrEmpty(user.PasswordHash);
    }

    private string GenerateJwtToken(Models.User user)
    {
        using var activity = SnackboxTelemetry.ActivitySource.StartActivity("GenerateJwtToken");
        activity?.SetTag("user.id", user.Id);
        
        _logger.LogDebug("Generating JWT token for user. UserId: {UserId}", user.Id);
        
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "SnackboxApi";
        var audience = jwtSettings["Audience"] ?? "SnackboxClient";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        _logger.LogDebug(
            "JWT settings - Issuer: {Issuer}, Audience: {Audience}, ExpirationMinutes: {ExpirationMinutes}",
            issuer,
            audience,
            expirationMinutes);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        _logger.LogDebug(
            "JWT token generated successfully. UserId: {UserId}, TokenLength: {TokenLength}, ExpiresAt: {ExpiresAt}",
            user.Id,
            tokenString.Length,
            token.ValidTo);

        return tokenString;
    }

    private static string MaskBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode) || barcode.Length <= 4)
            return "****";
        
        return $"{barcode[..2]}****{barcode[^2..]}";
    }
}
