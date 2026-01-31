using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public interface IUpdateService
{
    Task<UpdateCheckResultDto> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task<UpdateApplyResultDto> ApplyUpdateAsync(UpdateApplyRequestDto request, CancellationToken cancellationToken = default);
}
