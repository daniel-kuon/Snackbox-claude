using System.Text.Json;
using Refit;
using Snackbox.Api.Dtos;
using Snackbox.ApiClient;

namespace Snackbox.Components.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IAuthApi _authApi;
    private readonly IStorageService _storageService;
    private const string TokenKey = "auth_token";
    private const string UserInfoKey = "user_info";

    public AuthenticationService(IAuthApi authApi, IStorageService storageService)
    {
        _authApi = authApi;
        _storageService = storageService;
    }

    public async Task<LoginResult> LoginAsync(string barcodeValue)
    {
        try
        {
            var request = new LoginRequest { BarcodeValue = barcodeValue };
            var loginResponse = await _authApi.LoginAsync(request);

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
        catch (ApiException apiEx)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = await TryReadErrorFromApiException(apiEx) ?? "Invalid barcode or authentication failed"
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
            var request = new LoginRequest { Username = username, Password = password };
            var loginResponse = await _authApi.LoginAsync(request);

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
        catch (ApiException apiEx)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = await TryReadErrorFromApiException(apiEx) ?? "Invalid username or password"
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
            var request = new LoginRequest { BarcodeValue = barcodeValue, Password = password };
            var loginResponse = await _authApi.LoginAsync(request);

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
        catch (ApiException apiEx)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = await TryReadErrorFromApiException(apiEx) ?? "Invalid barcode or password"
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

    public async Task<OperationResult> SetPasswordAsync(string barcodeValue, string email, string newPassword)
    {
        try
        {
            var request = new SetPasswordRequest
            {
                BarcodeValue = barcodeValue,
                Email = email,
                NewPassword = newPassword
            };

            await _authApi.SetPasswordAsync(request);
            return new OperationResult { Success = true };
        }
        catch (ApiException apiEx)
        {
            var err = await TryReadErrorFromApiException(apiEx);
            return new OperationResult { Success = false, ErrorMessage = err ?? "Failed to set password" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<OperationResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            await _authApi.ChangePasswordAsync(request);
            return new OperationResult { Success = true };
        }
        catch (ApiException apiEx)
        {
            var err = await TryReadErrorFromApiException(apiEx);
            return new OperationResult { Success = false, ErrorMessage = err ?? "Failed to change password" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static async Task<string?> TryReadErrorFromApiException(ApiException apiException)
    {
        try
        {
            if (apiException.Content == null)
                return null;

            var error = JsonSerializer.Deserialize<ErrorResponse>(apiException.Content);
            return error?.Message;
        }
        catch
        {
            return null;
        }
    }
}
