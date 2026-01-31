using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Update API endpoints (Admin only)
/// </summary>
public interface IUpdatesApi
{
    [Get("/api/updates/check")]
    Task<UpdateCheckResultDto> CheckAsync();

    [Post("/api/updates/apply")]
    Task<ApiResponse<UpdateApplyResultDto>> ApplyAsync([Body] UpdateApplyRequestDto request);
}
