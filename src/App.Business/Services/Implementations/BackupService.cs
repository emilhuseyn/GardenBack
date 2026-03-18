using App.Business.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Creates daily MySQL database backups using mysqldump.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly string _connectionString;
        private readonly string _backupDirectory;

        public BackupService(ILogger<BackupService> logger, IConfiguration configuration)
        {
            _logger           = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("DefaultConnection tapılmadı.");
            _backupDirectory  = configuration["Backup:Directory"] ?? "Backups";
        }

        /// <summary>
        /// Parses a key=value; style connection string into a dictionary.
        /// </summary>
        private static Dictionary<string, string> ParseConnectionString(string cs)
        {
            return cs.Split(';', StringSplitOptions.RemoveEmptyEntries)
                     .Select(part => part.Split('=', 2))
                     .Where(kv => kv.Length == 2)
                     .ToDictionary(
                         kv => kv[0].Trim().ToLowerInvariant(),
                         kv => kv[1].Trim(),
                         StringComparer.OrdinalIgnoreCase);
        }

        public async Task CreateBackupAsync()
        {
            try
            {
                var parts    = ParseConnectionString(_connectionString);
                var server   = parts.GetValueOrDefault("server",   "localhost");
                var database = parts.GetValueOrDefault("database", "Garden");
                var user     = parts.GetValueOrDefault("user",     "root");
                var password = parts.GetValueOrDefault("password", string.Empty);

                var dir      = Path.GetFullPath(_backupDirectory);
                Directory.CreateDirectory(dir);

                var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                var filePath = Path.Combine(dir, fileName);

                var args = $"-h {server} -u {user} -p{password} {database}";

                var psi = new ProcessStartInfo
                {
                    FileName               = "mysqldump",
                    Arguments              = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var process = Process.Start(psi)
                    ?? throw new InvalidOperationException("mysqldump prosesi başladıla bilmədi.");

                var output = await process.StandardOutput.ReadToEndAsync();
                var error  = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("[Backup] mysqldump xətası: {Error}", error);
                    return;
                }

                await File.WriteAllTextAsync(filePath, output);
                _logger.LogInformation("[Backup] Uğurlu: {File}", filePath);

                DeleteOldBackups(dir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Backup] Backup zamanı xəta baş verdi.");
            }
        }

        /// <summary>
        /// Deletes backup files older than the retention period (default: 30 days).
        /// </summary>
        private void DeleteOldBackups(string directory)
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-30);
                foreach (var file in Directory.GetFiles(directory, "backup_*.sql"))
                {
                    if (File.GetCreationTime(file) < cutoff)
                    {
                        File.Delete(file);
                        _logger.LogInformation("[Backup] Köhnə backup silindi: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Backup] Köhnə backup silinərkən xəta.");
            }
        }
    }
}
