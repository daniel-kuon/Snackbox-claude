using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>Seasonal kiosk themes. Which one is on is anonymous (the kiosk); listing and changing them is admin only.</summary>
public interface IThemesApi
{
    [Get("/api/themes/active")]
    Task<ActiveThemeDto> GetActiveAsync();

    [Get("/api/themes")]
    Task<List<ThemeDto>> GetAllAsync();

    [Put("/api/themes/{key}")]
    Task<ThemeDto> UpdateAsync(string key, [Body] UpdateThemeDto dto);
}
