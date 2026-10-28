namespace Snackbox.Api.Services;

/// <summary>
/// Every hour, downloads a newer release's prebuilt package ahead of time, so pressing Install
/// on Admin -> Updates only stops Snackbox, swaps the files and starts it. Installations only:
/// a developer's checkout should not fill up with release downloads.
/// </summary>
public class UpdateDownloadBackgroundService(IServiceProvider services, IHostEnvironment environment, ILogger<UpdateDownloadBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsProduction()) return;

        try
        {
            // Not during startup, which is busy enough on the kiosk PC
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = services.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IUpdateService>().DownloadLatestAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Could not download the latest release in the background");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }
}
