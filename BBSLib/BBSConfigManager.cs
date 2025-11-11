using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace BBSLib
{
    /// <summary>
    /// Provides JSON (Config.json) persistence for BBSConfig.
    /// Uses DataContractJsonSerializer to remain compatible with .NET Framework4.8.1 (C#7.3).
    /// </summary>
    public class BBSConfigManager
    {
        /// <summary>Path of the config file (Config.json).</summary>
        public string ConfigPath { get; }
        /// <summary>Optional backup file count.</summary>
        public int BackupRetainCount { get; set; } = 3;

        public BBSConfigManager(string configDirectory = null)
        {
            var baseDir = configDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
            ConfigPath = Path.Combine(baseDir, "Config.json");
        }

        /// <summary>
        /// Load configuration from Config.json or create a new default if missing.
        /// </summary>
        public BBSConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return new BBSConfig();
                }
                using (var fs = File.OpenRead(ConfigPath))
                {
                    var ser = new DataContractJsonSerializer(typeof(BBSConfig));
                    return (BBSConfig)ser.ReadObject(fs);
                }
            }
            catch (Exception)
            {
                // Corrupt or unreadable file: rename and start fresh
                TryBackupCorrupt();
                return new BBSConfig();
            }
        }

        /// <summary>
        /// Try to load existing config; returns null if not present or corrupt.
        /// </summary>
        public BBSConfig TryLoad()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return null;
                using (var fs = File.OpenRead(ConfigPath))
                {
                    var ser = new DataContractJsonSerializer(typeof(BBSConfig));
                    return (BBSConfig)ser.ReadObject(fs);
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// Save configuration atomically (write temp then replace) and create timestamped backup of previous file.
        /// </summary>
        public void Save(BBSConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

            // Backup existing
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    var backupPath = ConfigPath + "." + ts + ".bak";
                    File.Copy(ConfigPath, backupPath, true);
                    TrimOldBackups();
                }
                catch { }
            }

            var temp = ConfigPath + ".tmp";
            try
            {
                using (var fs = File.Create(temp))
                {
                    var ser = new DataContractJsonSerializer(typeof(BBSConfig));
                    ser.WriteObject(fs, config);
                }
                File.Copy(temp, ConfigPath, true);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }

        private void TryBackupCorrupt()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                var corruptName = ConfigPath + ".corrupt_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                File.Move(ConfigPath, corruptName);
                TrimOldBackups();
            }
            catch { }
        }

        private void TrimOldBackups()
        {
            try
            {
                if (BackupRetainCount <= 0) return;
                var dir = Path.GetDirectoryName(ConfigPath);
                var prefix = Path.GetFileName(ConfigPath);
                var files = Directory.GetFiles(dir, prefix + ".*.bak");
                Array.Sort(files); // lexical sort includes timestamp order due to format
                if (files.Length > BackupRetainCount)
                {
                    for (int i = 0; i < files.Length - BackupRetainCount; i++)
                    {
                        try { File.Delete(files[i]); } catch { }
                    }
                }
            }
            catch { }
        }
    }
}
