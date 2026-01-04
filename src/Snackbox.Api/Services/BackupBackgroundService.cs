using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public class BackupBackgroundService : BackgroundService
{
    private readonly ILogger<BackupBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private DateTime _lastWeeklyEmailSent = DateTime.MinValue;

    public BackupBackgroundService(
        ILogger<BackupBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backup Background Service started");

        // Wait for initial delay before starting (to allow app startup to complete)
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformDailyBackupAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Backup Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in backup background service");
                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task PerformDailyBackupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            // Determine backup type based on day of week and month
            var now = DateTime.UtcNow;
            var backupType = DetermineBackupType(now);

            _logger.LogInformation("Creating {Type} backup", backupType);
            var backup = await backupService.CreateBackupAsync(backupType);

            // Send email once per week (on weekly backup day)
            if (backupType == BackupType.Weekly && ShouldSendWeeklyEmail())
            {
                await SendBackupEmailAsync(backupService, emailService, backup);
                _lastWeeklyEmailSent = DateTime.UtcNow;
            }

            // Run cleanup
            await backupService.CleanupOldBackupsAsync();

            _logger.LogInformation("Daily backup process completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform daily backup");
        }
    }

    private BackupType DetermineBackupType(DateTime date)
    {
        // First day of month = Monthly backup
        if (date.Day == 1)
        {
            return BackupType.Monthly;
        }

        // Sunday = Weekly backup
        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            return BackupType.Weekly;
        }

        // Otherwise = Daily backup
        return BackupType.Daily;
    }

    private bool ShouldSendWeeklyEmail()
    {
        // Send email if we haven't sent one in the last 6 days
        return (DateTime.UtcNow - _lastWeeklyEmailSent).TotalDays >= 6;
    }

    private async Task SendBackupEmailAsync(IBackupService backupService, IEmailService emailService, BackupMetadata backup)
    {
        try
        {
            var emailConfig = _configuration.GetSection("Email");
            var backupRecipient = emailConfig["BackupRecipient"];

            if (string.IsNullOrEmpty(backupRecipient))
            {
                _logger.LogWarning("Email:BackupRecipient is not configured, skipping email send");
                return;
            }

            _logger.LogInformation("Sending weekly backup email to {Recipient}", backupRecipient);

            var backupStream = await backupService.GetBackupStreamAsync(backup.Id);
            var subject = $"Snackbox Weekly Backup - {backup.CreatedAt:yyyy-MM-dd}";
            var body = $@"
                <html>
                <body>
                    <h2>Snackbox Weekly Backup</h2>
                    <p>This is your weekly database backup for Snackbox.</p>
                    <p><strong>Backup Details:</strong></p>
                    <ul>
                        <li>Date: {backup.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</li>
                        <li>File: {backup.FileName}</li>
                        <li>Size: {FormatFileSize(backup.FileSizeBytes)}</li>
                    </ul>
                    <p>The backup file is attached to this email.</p>
                    <p><em>This is an automated message from Snackbox Backup System.</em></p>
                </body>
                </html>
            ";

            await emailService.SendEmailAsync(backupRecipient, subject, body, backupStream, backup.FileName);
            
            // Dispose the stream after sending
            await backupStream.DisposeAsync();

            _logger.LogInformation("Weekly backup email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send backup email");
            // Don't throw - backup was successful even if email failed
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
