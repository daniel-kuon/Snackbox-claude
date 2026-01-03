using System.Net.Http.Json;
using System.Text.Json;

namespace Snackbox.Components.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;
    private const string TokenKey = "auth_token";
    private const string UserInfoKey = "user_info";

    public AuthenticationService(HttpClient httpClient, IStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
    }

    public async Task<LoginResult> LoginAsync(string barcodeValue)
    {
        try
        {
            var request = new { BarcodeValue = barcodeValue };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse != null)
                {
                    // Store token and user info securely
                    await _storageService.SetAsync(TokenKey, loginResponse.Token);
                    await _storageService.SetAsync(UserInfoKey, JsonSerializer.Serialize(loginResponse));

                    return new LoginResult
                    {
                        Success = true,
                        Token = loginResponse.Token,
                        Username = loginResponse.Username,
                        Email = loginResponse.Email,
                        IsAdmin = loginResponse.IsAdmin,
                        UserId = loginResponse.UserId
                    };
                }
            }

            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid barcode or authentication failed"
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = $"Error during login: {ex.Message}"
            };
        }
    }

    public async Task<LoginResult> LoginWithPasswordAsync(string username, string password)
    {
        try
        {
            var request = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse != null)
                {
                    // Store token and user info securely
                    await _storageService.SetAsync(TokenKey, loginResponse.Token);
                    await _storageService.SetAsync(UserInfoKey, JsonSerializer.Serialize(loginResponse));

                    return new LoginResult
                    {
                        Success = true,
                        Token = loginResponse.Token,
                        Username = loginResponse.Username,
                        Email = loginResponse.Email,
                        IsAdmin = loginResponse.IsAdmin,
                        UserId = loginResponse.UserId
                    };
                }
            }

            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = $"Error during login: {ex.Message}"
            };
        }
    }

    public async Task<LoginResult> LoginWithBarcodeAndPasswordAsync(string barcodeValue, string password)
    {
        try
        {
            var request = new { BarcodeValue = barcodeValue, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse != null)
                {
                    // Store token and user info securely
                    await _storageService.SetAsync(TokenKey, loginResponse.Token);
                    await _storageService.SetAsync(UserInfoKey, JsonSerializer.Serialize(loginResponse));

                    return new LoginResult
                    {
                        Success = true,
                        Token = loginResponse.Token,
                        Username = loginResponse.Username,
                        Email = loginResponse.Email,
                        IsAdmin = loginResponse.IsAdmin,
                        UserId = loginResponse.UserId
                    };
                }
            }

            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid barcode or password"
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = $"Error during login: {ex.Message}"
            };
        }
    }

    public async Task LogoutAsync()
    {
        _storageService.Remove(TokenKey);
        _storageService.Remove(UserInfoKey);
        await Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _storageService.GetAsync(TokenKey);
        }
        catch
        {
            return null;
        }
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public int UserId { get; set; }
    }
}
