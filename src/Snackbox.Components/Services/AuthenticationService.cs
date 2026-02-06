using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;

namespace Snackbox.Components.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;
    private readonly IUiTelemetry _uiTelemetry;
    private const string TokenKey = "auth_token";
    private const string UserInfoKey = "user_info";

    public AuthenticationService(HttpClient httpClient, IStorageService storageService, IUiTelemetry uiTelemetry)
    {
        _httpClient = httpClient;
        _storageService = storageService;
        _uiTelemetry = uiTelemetry;
    }

    public async Task<LoginResult> LoginAsync(string barcodeValue)
    {
        var activity = await _uiTelemetry.StartUiActionAsync(
            "login",
            component: nameof(AuthenticationService),
            tags: new Dictionary<string, object?> { ["auth.method"] = "barcode" });

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

                    activity?.SetTag("auth.success", true);
                    _uiTelemetry.SetUserTags(activity, new UserInfo
                    {
                        UserId = loginResponse.UserId,
                        Username = loginResponse.Username,
                        Email = loginResponse.Email,
                        IsAdmin = loginResponse.IsAdmin
                    });

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

            activity?.SetTag("auth.success", false);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid barcode or authentication failed"
            };
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = $"Error during login: {ex.Message}"
            };
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public async Task<LoginResult> LoginWithPasswordAsync(string username, string password)
    {
        var activity = await _uiTelemetry.StartUiActionAsync(
            "login",
            component: nameof(AuthenticationService),
            tags: new Dictionary<string, object?> { ["auth.method"] = "password" });

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

                    activity?.SetTag("auth.success", true);
                    _uiTelemetry.SetUserTags(activity, new UserInfo
                    {
                        UserId = loginResponse.UserId,
                        Username = loginResponse.Username,
                        Email = loginResponse.Email,
                        IsAdmin = loginResponse.IsAdmin
                    });

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

            activity?.SetTag("auth.success", false);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = $"Error during login: {ex.Message}"
            };
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public async Task<LoginResult> LoginWithBarcodeAndPasswordAsync(string barcodeValue, string password)
    {
        var activity = await _uiTelemetry.StartUiActionAsync(
            "login",
            component: nameof(AuthenticationService),
            tags: new Dictionary<string, object?> { ["auth.method"] = "barcode_password" });

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

                    activity?.SetTag("auth.success", true);
                    _uiTelemetry.SetUserTags(activity, new UserInfo
                    {
                        UserId = loginResponse.UserId,
                        Username = loginResponse.Username,
                        Email = loginResponse.Email,
                        IsAdmin = loginResponse.IsAdmin
                    });

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

            activity?.SetTag("auth.success", false);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid barcode or password"
            };
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = $"Error during login: {ex.Message}"
            };
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public async Task LogoutAsync()
    {
        var activity = await _uiTelemetry.StartUiActionAsync("logout", component: nameof(AuthenticationService));
        try
        {
            _storageService.Remove(TokenKey);
            _storageService.Remove(UserInfoKey);
            await Task.CompletedTask;
        }
        finally
        {
            activity?.Dispose();
        }
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

    public async Task<UserInfo?> GetCurrentUserInfoAsync()
    {
        try
        {
            var userInfoJson = await _storageService.GetAsync(UserInfoKey);
            if (string.IsNullOrEmpty(userInfoJson))
            {
                return null;
            }

            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(userInfoJson);
            if (loginResponse == null)
            {
                return null;
            }

            return new UserInfo
            {
                UserId = loginResponse.UserId,
                Username = loginResponse.Username,
                Email = loginResponse.Email,
                IsAdmin = loginResponse.IsAdmin
            };
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
