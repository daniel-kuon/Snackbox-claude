using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Import from, and comparison against, the old Snackbox SQL Server database. Admin only -
/// the request body carries that database's credentials.
/// </summary>
public interface ILegacyImportApi
{
    [Post("/api/legacyimport/test")]
    Task<LegacyConnectionTestDto> TestAsync([Body] LegacyConnectionDto connection);

    [Post("/api/legacyimport/import")]
    Task<LegacyImportResultDto> ImportAsync([Body] LegacyImportRequestDto request);

    [Post("/api/legacyimport/verify")]
    Task<LegacyVerificationDto> VerifyAsync([Body] LegacyVerificationRequestDto request);
}
