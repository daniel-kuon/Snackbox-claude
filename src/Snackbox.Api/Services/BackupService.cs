using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

public class BackupService : IBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _backupDirectory;
    private readonly string _connectionString;
    private readonly string _metadataFile;

    public BackupService(
        IConfiguration configuration,
        ILogger<BackupService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _backupDirectory = configuration["Backup:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "backups");
        _connectionString = configuration.GetConnectionString("snackboxdb")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
        _metadataFile = Path.Combine(_backupDirectory, "metadata.json");

        // Ensure backup directory exists
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<BackupMetadata> CreateBackupAsync(BackupType type)
    {
        _logger.LogInformation("Creating {Type} backup", type);

        var backupId = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{type}";
        var fileName = $"snackbox_backup_{backupId}.sql";
        var filePath = Path.Combine(_backupDirectory, fileName);

        // Parse connection string to get database connection details
        var connectionParams = ParseConnectionString(_connectionString);

        // Create pg_dump command
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h {connectionParams.Host} -p {connectionParams.Port} -U {connectionParams.Username} -d {connectionParams.Database} -F p -f \"{filePath}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        // Set password via environment variable
        process.StartInfo.Environment["PGPASSWORD"] = connectionParams.Password;

        try
        {
            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("pg_dump failed with exit code {ExitCode}: {Error}", process.ExitCode, stderr);
                throw new Exception($"Backup failed: {stderr}");
            }

            var fileInfo = new FileInfo(filePath);
            var metadata = new BackupMetadata
            {
                Id = backupId,
                FileName = fileName,
                CreatedAt = DateTime.UtcNow,
                Type = type,
                FileSizeBytes = fileInfo.Length
            };

            await SaveMetadataAsync(metadata);

            _logger.LogInformation("Backup created successfully: {FileName} ({Size} bytes)", fileName, fileInfo.Length);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            // Clean up partial backup file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            throw;
        }
    }

    public async Task<List<BackupMetadata>> ListBackupsAsync()
    {
        var metadataList = await LoadAllMetadataAsync();
        return metadataList.OrderByDescending(m => m.CreatedAt).ToList();
    }

    public async Task RestoreBackupAsync(string backupId)
    {
        _logger.LogInformation("Restoring backup: {BackupId}", backupId);

        var metadata = await GetBackupMetadataAsync(backupId);
        if (metadata == null)
        {
            throw new FileNotFoundException($"Backup with ID '{backupId}' not found");
        }

        var filePath = Path.Combine(_backupDirectory, metadata.FileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Backup file '{metadata.FileName}' not found on disk");
        }

        // Parse connection string
        var connectionParams = ParseConnectionString(_connectionString);

        // Drop and recreate the database
        await DropAndRecreateDatabaseAsync(connectionParams);

        // Restore from backup using psql
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h {connectionParams.Host} -p {connectionParams.Port} -U {connectionParams.Username} -d {connectionParams.Database} -f \"{filePath}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.StartInfo.Environment["PGPASSWORD"] = connectionParams.Password;

        try
        {
            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("psql completed with warnings: {Error}", stderr);
                // Don't throw - some warnings are normal during restore
            }

            _logger.LogInformation("Backup restored successfully: {BackupId}", backupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup");
            throw;
        }
    }

    public async Task<BackupMetadata> ImportBackupAsync(Stream fileStream, string fileName)
    {
        _logger.LogInformation("Importing backup from file: {FileName}", fileName);

        var backupId = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_imported";
        var newFileName = $"snackbox_backup_{backupId}.sql";
        var filePath = Path.Combine(_backupDirectory, newFileName);

        try
        {
            using (var fileStreamOut = File.Create(filePath))
            {
                await fileStream.CopyToAsync(fileStreamOut);
            }

            var fileInfo = new FileInfo(filePath);
            var metadata = new BackupMetadata
            {
                Id = backupId,
                FileName = newFileName,
                CreatedAt = DateTime.UtcNow,
                Type = BackupType.Manual,
                FileSizeBytes = fileInfo.Length
            };

            await SaveMetadataAsync(metadata);

            _logger.LogInformation("Backup imported successfully: {FileName}", newFileName);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import backup");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            throw;
        }
    }

    public async Task DeleteBackupAsync(string backupId)
    {
        _logger.LogInformation("Deleting backup: {BackupId}", backupId);

        var metadata = await GetBackupMetadataAsync(backupId);
        if (metadata == null)
        {
            throw new FileNotFoundException($"Backup with ID '{backupId}' not found");
        }

        var filePath = Path.Combine(_backupDirectory, metadata.FileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await RemoveMetadataAsync(backupId);

        _logger.LogInformation("Backup deleted successfully: {BackupId}", backupId);
    }

    public async Task CleanupOldBackupsAsync()
    {
        _logger.LogInformation("Running backup cleanup");

        var now = DateTime.UtcNow;
        var allBackups = await LoadAllMetadataAsync();

        foreach (var backup in allBackups)
        {
            var age = now - backup.CreatedAt;
            bool shouldDelete = backup.Type switch
            {
                BackupType.Daily => age.TotalDays > 30,
                BackupType.Weekly => age.TotalDays > 90,
                BackupType.Monthly => false, // Keep forever
                BackupType.Manual => false, // Keep manual backups
                _ => false
            };

            if (shouldDelete)
            {
                _logger.LogInformation("Deleting old {Type} backup: {Id} (age: {Days} days)", 
                    backup.Type, backup.Id, (int)age.TotalDays);
                await DeleteBackupAsync(backup.Id);
            }
        }

        _logger.LogInformation("Backup cleanup completed");
    }

    public async Task<Stream> GetBackupStreamAsync(string backupId)
    {
        var metadata = await GetBackupMetadataAsync(backupId);
        if (metadata == null)
        {
            throw new FileNotFoundException($"Backup with ID '{backupId}' not found");
        }

        var filePath = Path.Combine(_backupDirectory, metadata.FileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Backup file '{metadata.FileName}' not found on disk");
        }

        return File.OpenRead(filePath);
    }

    public async Task<bool> CheckDatabaseExistsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection check failed");
            return false;
        }
    }

    public async Task CreateEmptyDatabaseAsync()
    {
        _logger.LogInformation("Creating empty database");

        var connectionParams = ParseConnectionString(_connectionString);
        
        // Drop and recreate database
        await DropAndRecreateDatabaseAsync(connectionParams);

        // Apply migrations
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        _logger.LogInformation("Empty database created successfully");
    }

    public async Task CreateSeededDatabaseAsync()
    {
        _logger.LogInformation("Creating seeded database");

        await CreateEmptyDatabaseAsync();
        // Migrations already include seed data
        
        _logger.LogInformation("Seeded database created successfully");
    }

    private async Task DropAndRecreateDatabaseAsync(ConnectionParams connectionParams)
    {
        // Connect to postgres database to drop and recreate target database
        var postgresConnString = $"Host={connectionParams.Host};Port={connectionParams.Port};Username={connectionParams.Username};Password={connectionParams.Password};Database=postgres";

        using var scope = _serviceProvider.CreateScope();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(postgresConnString);
        
        using var tempContext = new ApplicationDbContext(optionsBuilder.Options);
        
        // Terminate existing connections
        // Note: Database name comes from configuration, not user input, so it's safe from SQL injection
        #pragma warning disable EF1002
        await tempContext.Database.ExecuteSqlRawAsync($@"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = '{connectionParams.Database}'
            AND pid <> pg_backend_pid();
        ");

        // Drop database
        await tempContext.Database.ExecuteSqlRawAsync($"DROP DATABASE IF EXISTS {connectionParams.Database};");
        
        // Create database
        await tempContext.Database.ExecuteSqlRawAsync($"CREATE DATABASE {connectionParams.Database};");
        #pragma warning restore EF1002
    }

    private async Task<BackupMetadata?> GetBackupMetadataAsync(string backupId)
    {
        var allMetadata = await LoadAllMetadataAsync();
        return allMetadata.FirstOrDefault(m => m.Id == backupId);
    }

    private async Task<List<BackupMetadata>> LoadAllMetadataAsync()
    {
        if (!File.Exists(_metadataFile))
        {
            return new List<BackupMetadata>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_metadataFile);
            return JsonSerializer.Deserialize<List<BackupMetadata>>(json) ?? new List<BackupMetadata>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load backup metadata");
            return new List<BackupMetadata>();
        }
    }

    private async Task SaveMetadataAsync(BackupMetadata metadata)
    {
        var allMetadata = await LoadAllMetadataAsync();
        allMetadata.Add(metadata);

        var json = JsonSerializer.Serialize(allMetadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_metadataFile, json);
    }

    private async Task RemoveMetadataAsync(string backupId)
    {
        var allMetadata = await LoadAllMetadataAsync();
        allMetadata.RemoveAll(m => m.Id == backupId);

        var json = JsonSerializer.Serialize(allMetadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_metadataFile, json);
    }

    private ConnectionParams ParseConnectionString(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        return new ConnectionParams
        {
            Host = builder.Host ?? "localhost",
            Port = builder.Port,
            Username = builder.Username ?? "postgres",
            Password = builder.Password ?? "",
            Database = builder.Database ?? "snackboxdb"
        };
    }

    private class ConnectionParams
    {
        public required string Host { get; set; }
        public int Port { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Database { get; set; }
    }
}
