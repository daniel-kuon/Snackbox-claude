using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>Release checks and in-place updates. Admin only.</summary>
public interface IUpdatesApi
{
    [Get("/api/updates/status")]
    Task<UpdateStatusDto> GetStatusAsync();

    [Post("/api/updates/install")]
    Task<InstallUpdateResponseDto> InstallAsync([Body] InstallUpdateRequestDto request);

    [Get("/api/updates/log")]
    Task<UpdateLogDto> GetLogAsync([Query] int lines = 200);
}
