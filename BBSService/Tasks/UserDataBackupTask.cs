using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BBSService.Tasks
{
    /// <summary>
    /// Periodically backups a user data folder to a backup location.
    /// </summary>
    public sealed class UserDataBackupTask : ServiceTaskBase
    {
        private readonly string _sourceDir;
        private readonly string _backupDir;
        private readonly TimeSpan _interval;
        public UserDataBackupTask(string sourceDir, string backupDir, TimeSpan interval) : base("UserDataBackup")
        {
            _sourceDir = sourceDir ?? string.Empty;
            _backupDir = backupDir ?? string.Empty;
            _interval = interval;
        }

        public override async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!Directory.Exists(_sourceDir))
                    {
                        Log("Source directory not found: " + _sourceDir);
                    }
                    else
                    {
                        Directory.CreateDirectory(_backupDir);
                        // simple mirror copy (shallow recursion). For large data sets, consider robocopy or incremental.
                        foreach (var file in Directory.EnumerateFiles(_sourceDir, "*", SearchOption.AllDirectories))
                        {
                            var rel = file.Substring(_sourceDir.TrimEnd(Path.DirectorySeparatorChar).Length).TrimStart(Path.DirectorySeparatorChar);
                            var dest = Path.Combine(_backupDir, rel);
                            Directory.CreateDirectory(Path.GetDirectoryName(dest));
                            File.Copy(file, dest, true);
                        }
                        Log("Backup completed to: " + _backupDir);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex.Message);
                }
                await Task.Delay(_interval, ct).ConfigureAwait(false);
            }
        }
    }
}
