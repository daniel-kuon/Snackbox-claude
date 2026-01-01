namespace Snackbox.Components.Services;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(string barcodeValue);
    Task<LoginResult> LoginWithPasswordAsync(string username, string password);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetTokenAsync();
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
    public int UserId { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string? ErrorMessage { get; set; }
}
