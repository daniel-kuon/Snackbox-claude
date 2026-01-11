namespace Snackbox.Components.Services;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(string barcodeValue);
    Task<LoginResult> LoginWithPasswordAsync(string username, string password);
    Task<LoginResult> LoginWithBarcodeAndPasswordAsync(string barcodeValue, string password);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetTokenAsync();
    Task<UserInfo?> GetCurrentUserInfoAsync();
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
    public int UserId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UserInfo
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
}
