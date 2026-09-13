using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Feature flag endpoints. Reading is anonymous (the kiosk has no logged-in user);
/// toggling requires an admin token.
/// </summary>
public interface IFeatureFlagsApi
{
    [Get("/api/featureflags")]
    Task<List<FeatureFlagDto>> GetAllAsync();

    [Put("/api/featureflags/{key}")]
    Task<FeatureFlagDto> UpdateAsync(string key, [Body] UpdateFeatureFlagDto dto);
}
