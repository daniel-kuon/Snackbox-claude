using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthenticationService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> AuthenticateAsync(string barcodeValue)
    {
        // Find the barcode with the user
        var barcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Code == barcodeValue && b.IsActive);

        if (barcode == null)
        {
            return null;
        }

        // Only allow login using login-only barcodes
        if (!barcode.IsLoginOnly)
        {
            return null;
        }

        // Generate JWT token
        var token = GenerateJwtToken(barcode.User);

        return new LoginResponse
        {
            Token = token,
            Username = barcode.User.Username,
            Email = barcode.User.Email,
            IsAdmin = barcode.User.IsAdmin,
            UserId = barcode.User.Id
        };
    }

    public async Task<LoginResponse?> AuthenticateWithPasswordAsync(string emailOrUsername, string password)
    {
        // Allow login with either email or username
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == emailOrUsername || u.Email == emailOrUsername);

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

    public async Task<LoginResponse?> AuthenticateWithBarcodeAndPasswordAsync(string barcodeValue, string password)
    {
        // Find the barcode with the user
        var barcode = await _context.Barcodes
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Code == barcodeValue && b.IsActive);

        if (barcode == null || string.IsNullOrEmpty(barcode.User.PasswordHash))
        {
            return null;
        }

        // Only allow login using login-only barcodes
        if (!barcode.IsLoginOnly)
        {
            return null;
        }

        // Verify password with BCrypt
        if (!BCrypt.Net.BCrypt.Verify(password, barcode.User.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(barcode.User);

        return new LoginResponse
        {
            Token = token,
            Username = barcode.User.Username,
            Email = barcode.User.Email,
            IsAdmin = barcode.User.IsAdmin,
            UserId = barcode.User.Id
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

        // Check if user already has a password - prevent overwriting
        if (!string.IsNullOrEmpty(barcode.User.PasswordHash))
        {
            throw new InvalidOperationException("User already has a password set. Cannot overwrite existing password.");
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

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "SnackboxApi";
        var audience = jwtSettings["Audience"] ?? "SnackboxClient";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };

        // Only add email claim if email is provided
        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
