using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Settings API endpoints (Admin only)
/// </summary>
public interface ISettingsApi
{
    [Get("/api/settings")]
    Task<ApplicationSettingsDto> GetSettingsAsync();

    [Put("/api/settings")]
    Task<ApiResponse<object>> UpdateSettingsAsync([Body] ApplicationSettingsDto settings);

    [Post("/api/settings/test/email")]
    Task<TestResultDto> TestEmailSettingsAsync([Body] EmailSettingsDto settings);

    [Post("/api/settings/test/barcode")]
    Task<TestResultDto> TestBarcodeLookupAsync([Body] BarcodeLookupSettingsDto settings);

    [Post("/api/settings/test/backup")]
    Task<TestResultDto> TestBackupSettingsAsync([Body] BackupSettingsDto settings);
}
