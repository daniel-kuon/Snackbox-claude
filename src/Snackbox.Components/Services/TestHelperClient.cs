using System.Net.Http.Json;
using System.Text.Json;
using Snackbox.Api.Dtos;

namespace Snackbox.Components.Services;

/// <summary>
/// Client for the development-only UI test helper. The API answers its endpoints only
/// when running in Development with TestHelper:Enabled - everywhere else GetUsersAsync
/// returns null and the UI hides all test helper controls.
/// </summary>
public class TestHelperClient
{
    // Same storage keys AuthenticationService uses, so a bypass login behaves like a real one
    private const string TokenKey = "auth_token";
    private const string UserInfoKey = "user_info";

    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;

    public TestHelperClient(HttpClient httpClient, IStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
    }

    /// <summary>Returns all users with their barcodes, or null when the helper is disabled.</summary>
    public async Task<List<TestUserDto>?> GetUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/testhelper/users");
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<List<TestUserDto>>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Drops, migrates and reseeds the database. Returns false when it fails or the helper is disabled.</summary>
    public async Task<bool> ResetDatabaseAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/testhelper/reset-database", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Logs in as the given user without a password and stores the token.</summary>
    public async Task<LoginResult> LoginAsAsync(int userId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/testhelper/login", new TestLoginRequest { UserId = userId });
            if (!response.IsSuccessStatusCode)
                return new LoginResult { Success = false, ErrorMessage = "Test login failed" };

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse == null)
                return new LoginResult { Success = false, ErrorMessage = "Invalid response" };

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
        catch (Exception ex)
        {
            return new LoginResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
