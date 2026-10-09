using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Users API endpoints
/// </summary>
public interface IUsersApi
{
    [Get("/api/users")]
    Task<IEnumerable<UserDto>> GetAllAsync([Query] bool? includeRetired = null);

    [Get("/api/users/{id}")]
    Task<UserDto> GetByIdAsync(int id);

    [Post("/api/users/register")]
    Task<RegisterResponse> RegisterAsync([Body] RegisterUserDto dto);

    [Post("/api/users")]
    Task<UserDto> CreateAsync([Body] CreateUserDto dto);

    [Put("/api/users/{id}")]
    Task<UserDto> UpdateAsync(int id, [Body] UpdateUserDto dto);

    [Delete("/api/users/{id}")]
    Task DeleteAsync(int id);

    [Post("/api/users/{id}/retire")]
    Task<UserDto> RetireAsync(int id);

    // Admin-only: set password for a specific user
    [Post("/api/users/{id}/set-password")]
    Task AdminSetPasswordAsync(int id, [Body] AdminSetPasswordRequest request);

    // Admin-only: fold a user (typically a replacement card) into another; the source is deleted
    [Post("/api/users/{id}/merge-into/{targetId}")]
    Task MergeIntoAsync(int id, int targetId);

    // Kiosk: complete setup of an inactive placeholder account (barcode = proof of card possession)
    [Post("/api/users/setup")]
    Task<CompleteAccountSetupResponse> CompleteSetupAsync([Body] CompleteAccountSetupDto dto);

    // Kiosk: record which introduction steps the card's user has now seen
    [Post("/api/users/wizard-steps-seen")]
    Task MarkWizardStepsSeenAsync([Body] MarkWizardStepsSeenDto dto);
}

public class RegisterResponse
{
    public string? Message { get; set; }
    public int UserId { get; set; }
}
