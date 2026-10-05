using Snackbox.Api.Dtos;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Models;

namespace Snackbox.Api.Services;

[Traced] // long-running pg_dump/psql work - worth seeing duration and failures
public class BackupService : IBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _backupDirectory;
    private readonly string _postgresContainer;
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
        string? configBackupDir = configuration["Backup:Directory"];
        if (configBackupDir is not null)
        {
            configBackupDir = configBackupDir.Trim();
        }
        // Backups must outlive the installation: it is a git checkout that gets replaced,
        // moved or reinstalled, so they default to ProgramData rather than a folder inside it.
        // A relative Backup:Directory is resolved against that same root for the same reason.
        var backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Snackbox");
        _backupDirectory = string.IsNullOrWhiteSpace(configBackupDir)
            ? Path.Combine(backupRoot, "backups")
            : Path.IsPathRooted(configBackupDir)
                ? configBackupDir
                : Path.Combine(backupRoot, configBackupDir);
        // The AppHost gives the database container this fixed name (Program.cs there)
        _postgresContainer = configuration["Backup:PostgresContainer"] ?? "snackbox-postgres";
        _connectionString = configuration.GetConnectionString("snackboxdb")
            ?? throw new InvalidOperationException("Database connection string is not configured.");
        _metadataFile = Path.Combine(_backupDirectory, "metadata.json");

        // Ensure backup directory exists
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<BackupMetadata?> CreateBackupAsync(BackupType type, string? customName = null)
    {
        _logger.LogInformation("Creating {Type} backup", type);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupId = $"{timestamp}_{type}";
        var fileNameBase = $"snackbox_backup_{backupId}";

        if (!string.IsNullOrWhiteSpace(customName))
        {
            // Sanitize custom name
            var sanitizedName = string.Join("_", customName.Split(Path.GetInvalidFileNameChars()))
                .Replace(" ", "_");
            fileNameBase = $"{fileNameBase}_{sanitizedName}";
        }

        var fileName = $"{fileNameBase}.sql";
        var filePath = Path.Combine(_backupDirectory, fileName);

        // Parse connection string to get database connection details
        var connectionParams = ParseConnectionString(_connectionString);
        ValidateConnectionParams(connectionParams);

        using var process = new Process { StartInfo = PostgresTool("pg_dump", connectionParams, input: false) };
        process.StartInfo.ArgumentList.Add("--format=plain");

        try
        {
            process.Start();

            // pg_dump writes the dump to stdout inside the container; stream it into the file
            // here, reading stderr at the same time so neither pipe can fill up and block.
            var stderrTask = process.StandardError.ReadToEndAsync();
            await using (var file = File.Create(filePath))
            {
                await process.StandardOutput.BaseStream.CopyToAsync(file);
            }
            var stderr = await stderrTask;
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("pg_dump failed with exit code {ExitCode}: {Error}", process.ExitCode, stderr);
                File.Delete(filePath);
                throw new Exception($"Backup failed: {stderr}");
            }

            var fileInfo = new FileInfo(filePath);

            // Calculate MD5 hash
            var md5Hash = await CalculateMd5HashAsync(filePath);

            // Check if backup with same hash already exists
            var existingBackups = await ListBackupsAsync();
            // The new file is already in the folder, so it would match itself and every backup
            // was thrown away as a "duplicate" of itself - compare against the others only.
            var duplicateBackup = existingBackups.FirstOrDefault(b => b.Md5Hash == md5Hash && b.FileName != fileName);

            if (duplicateBackup != null)
            {
                _logger.LogInformation("Backup is identical to existing backup {ExistingId}, deleting duplicate", duplicateBackup.Id);

                // Delete the newly created backup file
                File.Delete(filePath);

                // Return null to indicate duplicate
                return null;
            }

            var metadata = ParseBackupFileName(fileName);
            if (metadata == null)
            {
                _logger.LogError("Failed to parse backup filename: {FileName}", fileName);
                throw new InvalidOperationException($"Failed to parse backup filename: {fileName}");
            }
            
            metadata.FileSizeBytes = fileInfo.Length;
            metadata.Md5Hash = md5Hash;

            _logger.LogInformation("Backup created successfully: {FileName} ({Size} bytes)", fileName, fileInfo.Length);
            return metadata;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Could not run docker to reach the database container {Container}", _postgresContainer);
            throw new InvalidOperationException($"Docker is not available, so the database container {_postgresContainer} cannot be reached.", ex);
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
        var backups = new List<BackupMetadata>();
        if (!Directory.Exists(_backupDirectory)) return backups;

        var files = Directory.GetFiles(_backupDirectory, "snackbox_backup_*.sql");
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var metadata = ParseBackupFileName(fileName);
            if (metadata != null)
            {
                var fileInfo = new FileInfo(file);
                metadata.FileSizeBytes = fileInfo.Length;
                metadata.Md5Hash = await CalculateMd5HashAsync(file);
                backups.Add(metadata);
            }
        }

        return backups.OrderByDescending(m => m.CreatedAt).ToList();
    }

    public async Task RestoreBackupAsync(string backupId, bool createBackupBeforeRestore = false)
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

        // Create backup before restore if requested
        if (createBackupBeforeRestore)
        {
            _logger.LogInformation("Creating backup of current database before restore");
            try
            {
                var preRestoreBackup = await CreateBackupAsync(BackupType.Manual);
                _logger.LogInformation("Pre-restore backup created: {BackupId}", preRestoreBackup.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create pre-restore backup, continuing with restore");
            }
        }

        // Parse connection string
        var connectionParams = ParseConnectionString(_connectionString);
        ValidateConnectionParams(connectionParams);

        // Drop and recreate the database
        await DropAndRecreateDatabaseAsync(connectionParams);

        using var process = new Process { StartInfo = PostgresTool("psql", connectionParams, input: true) };
        // Stop at the first error and report it. This used to carry on and log a warning, so
        // a restore that failed half-way was reported as a success - over an emptied database.
        process.StartInfo.ArgumentList.Add("--set=ON_ERROR_STOP=1");
        process.StartInfo.ArgumentList.Add("--quiet");

        try
        {
            process.Start();

            // The dump goes in through stdin: psql runs inside the container, which cannot
            // see a file on this machine.
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await using (var file = File.OpenRead(filePath))
            {
                await file.CopyToAsync(process.StandardInput.BaseStream);
            }
            process.StandardInput.Close();
            await stdoutTask;
            var stderr = await stderrTask;
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("psql failed with exit code {ExitCode}: {Error}", process.ExitCode, stderr);
                throw new Exception($"Restore failed: {stderr}");
            }

            _logger.LogInformation("Backup restored successfully: {BackupId}", backupId);

            // Apply outstanding migrations after restore
            await ApplyMigrationsAsync();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Could not run docker to reach the database container {Container}", _postgresContainer);
            throw new InvalidOperationException($"Docker is not available, so the database container {_postgresContainer} cannot be reached.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup");
            throw;
        }
    }

    private async Task ApplyMigrationsAsync()
    {
        _logger.LogInformation("Applying outstanding migrations...");

        const int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();

                _logger.LogInformation("Migrations applied successfully.");
                return;
            }
            catch (Npgsql.NpgsqlException ex) when (i < maxRetries - 1)
            {
                _logger.LogWarning(ex, "Failed to apply migrations (attempt {Attempt}/{MaxRetries}), retrying...", i + 1, maxRetries);
                await Task.Delay(2000); // Wait 2 seconds before retry
            }
        }
    }

    public async Task<BackupMetadata> ImportBackupAsync(Stream fileStream, string fileName)
    {
        _logger.LogInformation("Importing backup from file: {FileName}", fileName);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupId = $"{timestamp}_Manual";
        var newFileName = $"snackbox_backup_{backupId}_imported.sql";
        var filePath = Path.Combine(_backupDirectory, newFileName);

        try
        {
            using (var fileStreamOut = File.Create(filePath))
            {
                await fileStream.CopyToAsync(fileStreamOut);
            }

            var fileInfo = new FileInfo(filePath);
            var metadata = ParseBackupFileName(newFileName);
            if (metadata != null)
            {
                metadata.FileSizeBytes = fileInfo.Length;
                metadata.Md5Hash = await CalculateMd5HashAsync(filePath);
            }

            _logger.LogInformation("Backup imported successfully: {FileName}", newFileName);
            return metadata ?? throw new Exception("Failed to parse imported backup metadata");
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

        _logger.LogInformation("Backup deleted successfully: {BackupId}", backupId);
    }

    public async Task CleanupOldBackupsAsync()
    {
        _logger.LogInformation("Running backup cleanup");

        var now = DateTime.UtcNow;
        var allBackups = await ListBackupsAsync();

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

            // First check if we can connect - with retry for Aspire timing issues
            var canConnect = false;
            Exception? lastException = null;

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    canConnect = await dbContext.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        _logger.LogInformation("Database connection successful on attempt {Attempt}", i + 1);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("Database connection attempt {Attempt} failed: {Message}", i + 1, ex.Message);
                    if (i < 4) await Task.Delay(1000);
                }
            }

            if (!canConnect)
            {
                _logger.LogWarning("Cannot connect to database after 5 attempts. Last error: {Error}",
                    lastException?.Message ?? "Unknown");
                return false;
            }

            // Simply try to query the Users table - if it exists, database is initialized
            try
            {
                var userCount = await dbContext.Users.CountAsync();
                _logger.LogInformation("Database initialized - Users table exists with {Count} users", userCount);
                return true;
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01") // undefined_table
            {
                _logger.LogInformation("Users table does not exist - database not initialized");
                return false;
            }
            catch (Exception ex) when (ex.Message.Contains("does not exist") || ex.Message.Contains("relation"))
            {
                _logger.LogInformation("Users table does not exist - database not initialized: {Message}", ex.Message);
                return false;
            }
        }
        catch (Npgsql.NpgsqlException ex) when (ex.Message.Contains("does not exist"))
        {
            _logger.LogInformation("Database does not exist: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection check failed: {Message}", ex.Message);
            return false;
        }
    }

    public async Task CreateEmptyDatabaseAsync(InitialAdminDto admin)
    {
        _logger.LogInformation("Creating empty database with initial admin {Username}", admin.Username);

        var connectionParams = ParseConnectionString(_connectionString);

        // Drop and recreate database
        await DropAndRecreateDatabaseAsync(connectionParams);

        // Wait a moment for the database to be fully created
        await Task.Delay(1000);

        // Apply migrations with retry logic (includes achievements from OnModelCreating)
        await ApplyMigrationsAsync();

        // An empty installation has nobody who could log in, and a remote one has nobody who
        // could scan a card - so the first administrator is created here, with a password.
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Users.Add(new User
        {
            Username = admin.Username.Trim(),
            Email = admin.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(admin.Password),
            IsAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    public async Task CreateSeededDatabaseAsync()
    {
        _logger.LogInformation("Creating seeded database");

        var connectionParams = ParseConnectionString(_connectionString);

        // Drop and recreate database
        await DropAndRecreateDatabaseAsync(connectionParams);

        // Wait a moment for the database to be fully created
        await Task.Delay(1000);

        // Apply migrations with retry logic
        await ApplyMigrationsAsync();

        // Seed sample data using the seeder service
        using var scope = _serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedSampleDataAsync();

        _logger.LogInformation("Seeded database created successfully with all sample data");
    }

    private async Task DropAndRecreateDatabaseAsync(ConnectionParams connectionParams)
    {
        // Validate parameters to prevent SQL injection
        ValidateConnectionParams(connectionParams);

        // Connect to postgres database to drop and recreate target database
        var postgresConnString = $"Host={connectionParams.Host};Port={connectionParams.Port};Username={connectionParams.Username};Password={connectionParams.Password};Database=postgres";

        using var scope = _serviceProvider.CreateScope();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(postgresConnString);

        using var tempContext = new ApplicationDbContext(optionsBuilder.Options);

        // Terminate existing connections
        // Note: Database name has been validated by ValidateConnectionParams
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
        var allMetadata = await ListBackupsAsync();
        return allMetadata.FirstOrDefault(m => m.Id == backupId);
    }

    private BackupMetadata? ParseBackupFileName(string fileName)
    {
        // Expected format: snackbox_backup_20260115_223652_Manual[_custom_name].sql
        var pattern = @"snackbox_backup_(\d{8})_(\d{6})_(\w+)(?:_(.+))?\.sql";
        var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern);

        if (!match.Success) return null;

        var dateStr = match.Groups[1].Value;
        var timeStr = match.Groups[2].Value;
        var typeStr = match.Groups[3].Value;
        var customName = match.Groups.Count > 4 ? match.Groups[4].Value : null;

        if (!Enum.TryParse<BackupType>(typeStr, out var type))
        {
            type = BackupType.Manual;
        }

        DateTime createdAt;
        try
        {
            createdAt = DateTime.ParseExact($"{dateStr}_{timeStr}", "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();
        }
        catch
        {
            createdAt = File.GetCreationTimeUtc(Path.Combine(_backupDirectory, fileName));
        }

        return new BackupMetadata
        {
            Id = $"{dateStr}_{timeStr}_{typeStr}",
            FileName = fileName,
            CreatedAt = createdAt,
            Type = type,
            CustomName = customName?.Replace("_", " "),
            FileSizeBytes = File.Exists(Path.Combine(_backupDirectory, fileName)) ? new FileInfo(Path.Combine(_backupDirectory, fileName)).Length : 0
        };
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

    private void ValidateConnectionParams(ConnectionParams connectionParams)
    {
        // Validate database name to prevent SQL injection
        // PostgreSQL identifiers can contain letters, digits, and underscores, and start with a letter or underscore
        if (string.IsNullOrWhiteSpace(connectionParams.Database) ||
            !System.Text.RegularExpressions.Regex.IsMatch(connectionParams.Database, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            throw new ArgumentException("Invalid database name. Database name must contain only letters, numbers, and underscores, and start with a letter or underscore.");
        }

        // Validate other parameters to prevent command injection
        if (string.IsNullOrWhiteSpace(connectionParams.Host))
        {
            throw new ArgumentException("Host cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(connectionParams.Username))
        {
            throw new ArgumentException("Username cannot be empty.");
        }
    }

    private class ConnectionParams
    {
        public required string Host { get; set; }
        public int Port { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Database { get; set; }
    }

    public async Task<bool> ArePostgresToolsAvailableAsync()
    {
        try
        {
            // The tools come with the database container, so "available" means docker can
            // reach that container and run pg_dump in it.
            using var check = new Process
            {
                StartInfo = new ProcessStartInfo("docker")
                {
                    ArgumentList = { "exec", _postgresContainer, "pg_dump", "--version" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            check.Start();
            var version = await check.StandardOutput.ReadToEndAsync();
            var error = await check.StandardError.ReadToEndAsync();
            await check.WaitForExitAsync();

            if (check.ExitCode != 0)
            {
                _logger.LogWarning("Cannot run pg_dump in container {Container}: {Error}", _postgresContainer, error);
                return false;
            }

            _logger.LogInformation("Backups use {Version} in container {Container}", version.Trim(), _postgresContainer);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Docker is not available to reach the database container {Container}", _postgresContainer);
            return false;
        }
    }

    /// <summary>
    /// pg_dump/psql run inside the database container rather than from this machine: the
    /// client then always matches the server's major version (pg_dump refuses to dump a newer
    /// server, so host tools broke every backup the moment Postgres was upgraded), and the
    /// installation needs no PostgreSQL install of its own. The password travels as an
    /// environment variable that docker forwards by name, never on a command line.
    /// </summary>
    private ProcessStartInfo PostgresTool(string tool, ConnectionParams connection, bool input)
    {
        var info = new ProcessStartInfo("docker")
        {
            RedirectStandardInput = input,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        info.ArgumentList.Add("exec");
        if (input) info.ArgumentList.Add("--interactive");
        info.ArgumentList.Add("--env");
        info.ArgumentList.Add("PGPASSWORD");
        info.ArgumentList.Add(_postgresContainer);
        info.ArgumentList.Add(tool);
        info.ArgumentList.Add("--username");
        info.ArgumentList.Add(connection.Username);
        info.ArgumentList.Add("--dbname");
        info.ArgumentList.Add(connection.Database);
        info.Environment["PGPASSWORD"] = connection.Password;
        return info;
    }

    private async Task<string> CalculateMd5HashAsync(string filePath)
    {
        using var md5 = MD5.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
