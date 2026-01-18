using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public interface ISettingsService
{
    ApplicationSettingsDto GetSettings();
    Task<bool> UpdateSettingsAsync(ApplicationSettingsDto settings);
    Task<TestResultDto> TestEmailSettingsAsync(EmailSettingsDto settings);
    Task<TestResultDto> TestBarcodeLookupAsync(BarcodeLookupSettingsDto settings);
    Task<TestResultDto> TestBackupSettingsAsync(BackupSettingsDto settings);
}
