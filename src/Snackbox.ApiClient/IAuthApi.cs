using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Authentication API endpoints
/// </summary>
public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request);

    [Post("/api/auth/set-password")]
    Task SetPasswordAsync([Body] SetPasswordRequest request);

    [Post("/api/auth/change-password")]
    Task ChangePasswordAsync([Body] ChangePasswordRequest request);

    [Get("/api/auth/has-password/{username}")]
    Task<HasPasswordResponse> HasPasswordAsync(string username);
}

public class HasPasswordResponse
{
    public bool HasPassword { get; set; }
}
