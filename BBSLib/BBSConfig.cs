using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace BBSLib
{
    /// <summary>
    /// Central application configuration containing common security-related settings.
    /// .NET Framework4.8.1 compatible (XML serialization helpers provided).
    /// </summary>
    [Serializable]
    public class BBSConfig
    {
        public string Version { get; set; }

        public FirewallConfig Firewall { get; set; }
        public DefenderConfig Defender { get; set; }
        public SmartScreenConfig SmartScreen { get; set; }
        public TLSConfig TLS { get; set; }
        public ProxyConfig Proxy { get; set; }
        public UpdateConfig Update { get; set; }
        public BackupConfig Backup { get; set; }
        public LoggingConfig Logging { get; set; }
        public DriverInstallConfig DriverInstall { get; set; }
        public ServiceConfig Service { get; set; }

        /// <summary>
        /// Thumbprints of trusted certificates (hex string without spaces).
        /// </summary>
        public List<string> TrustedCertificatesThumbprints { get; set; }

        public BBSConfig()
        {
            Version = "1.0";
            Firewall = new FirewallConfig();
            Defender = new DefenderConfig();
            SmartScreen = new SmartScreenConfig();
            TLS = new TLSConfig();
            Proxy = new ProxyConfig();
            Update = new UpdateConfig();
            Backup = new BackupConfig();
            Logging = new LoggingConfig();
            DriverInstall = new DriverInstallConfig();
            Service = new ServiceConfig();
            TrustedCertificatesThumbprints = new List<string>();
        }

        public void Save(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var ser = new XmlSerializer(typeof(BBSConfig));
            using (var fs = File.Create(filePath))
            {
                ser.Serialize(fs, this);
            }
        }

        public static BBSConfig Load(string filePath)
        {
            var ser = new XmlSerializer(typeof(BBSConfig));
            using (var fs = File.OpenRead(filePath))
            {
                return (BBSConfig)ser.Deserialize(fs);
            }
        }

        /// <summary>
        /// Validate settings. Returns warnings/errors as human-friendly strings.
        /// </summary>
        public List<string> Validate()
        {
            var issues = new List<string>();
            if (DriverInstall != null)
            {
                if (DriverInstall.AllowUnsignedDrivers)
                    issues.Add("Warning: AllowUnsignedDrivers is enabled. This reduces system security.");
                if (DriverInstall.AllowTestSignedDrivers)
                    issues.Add("Warning: AllowTestSignedDrivers is enabled. Use only in development.");
            }
            if (TLS != null && TLS.MinimumTLSVersion < 1.2m)
            {
                issues.Add("Warning: TLS minimum version below1.2 might be insecure.");
            }
            return issues;
        }
    }

    #region Sections

    [Serializable]
    public class FirewallConfig
    {
        /// <summary>
        /// Whether to ensure firewall is enabled on all profiles.
        /// </summary>
        public bool EnforceEnabled { get; set; }
        /// <summary>
        /// Per-application allow rules.
        /// </summary>
        public List<FirewallAppRule> AllowedApps { get; set; }

        public FirewallConfig()
        {
            EnforceEnabled = true;
            AllowedApps = new List<FirewallAppRule>();
        }
    }

    [Serializable]
    public class FirewallAppRule
    {
        public string Name { get; set; }
        public string Path { get; set; }
        /// <summary>
        /// Profiles: Domain, Private, Public (comma separated)
        /// </summary>
        public string Profiles { get; set; }
        /// <summary>
        /// Direction: Inbound/Outbound
        /// </summary>
        public string Direction { get; set; }
        public bool Enabled { get; set; }

        public FirewallAppRule()
        {
            Enabled = true;
            Direction = "Inbound";
            Profiles = "Domain,Private,Public";
        }
    }

    [Serializable]
    public class DefenderConfig
    {
        /// <summary>
        /// Real-time protection enabled.
        /// </summary>
        public bool RealTimeProtection { get; set; }
        /// <summary>
        /// Cloud-delivered protection enabled.
        /// </summary>
        public bool CloudProtection { get; set; }
        /// <summary>
        /// Periodic scan schedule in hours (0 to disable scheduling here).
        /// </summary>
        public int PeriodicScanHours { get; set; }
        /// <summary>
        /// Paths excluded from scan.
        /// </summary>
        public List<string> Exclusions { get; set; }

        public DefenderConfig()
        {
            RealTimeProtection = true;
            CloudProtection = true;
            PeriodicScanHours = 0;
            Exclusions = new List<string>();
        }
    }

    [Serializable]
    public class SmartScreenConfig
    {
        public bool CheckAppsAndFiles { get; set; }
        public bool CheckDownloads { get; set; }
        public bool BlockUnrecognizedApps { get; set; }

        public SmartScreenConfig()
        {
            CheckAppsAndFiles = true;
            CheckDownloads = true;
            BlockUnrecognizedApps = true;
        }
    }

    [Serializable]
    public class TLSConfig
    {
        /// <summary>
        /// Minimum TLS version allowed (e.g.,1.0,1.1,1.2,1.3). Use decimal to keep XML simple.
        /// </summary>
        public decimal MinimumTLSVersion { get; set; }
        /// <summary>
        /// Allow legacy SSL/TLS protocols.
        /// </summary>
        public bool AllowLegacyProtocols { get; set; }
        public bool EnableStrongCrypto { get; set; }

        public TLSConfig()
        {
            MinimumTLSVersion = 1.2m;
            AllowLegacyProtocols = false;
            EnableStrongCrypto = true;
        }
    }

    [Serializable]
    public class ProxyConfig
    {
        public bool UseSystemProxy { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public ProxyConfig()
        {
            UseSystemProxy = true;
            Address = string.Empty;
            Port = 0;
            Username = string.Empty;
            Password = string.Empty;
        }
    }

    [Serializable]
    public class UpdateConfig
    {
        public string Channel { get; set; } // Stable/Beta/Dev
        public bool AutoCheck { get; set; }
        public int CheckIntervalHours { get; set; }
        public string Endpoint { get; set; }

        public UpdateConfig()
        {
            Channel = "Stable";
            AutoCheck = true;
            CheckIntervalHours = 24;
            Endpoint = string.Empty;
        }
    }

    [Serializable]
    public class BackupConfig
    {
        public string SourceDirectory { get; set; }
        public string BackupDirectory { get; set; }
        public int IntervalHours { get; set; }
        public bool UseCompression { get; set; }

        public BackupConfig()
        {
            SourceDirectory = string.Empty;
            BackupDirectory = string.Empty;
            IntervalHours = 24;
            UseCompression = false;
        }
    }

    [Serializable]
    public class LoggingConfig
    {
        public string Level { get; set; } // Trace/Debug/Info/Warn/Error
        public string LogDirectory { get; set; }
        public long MaxFileBytes { get; set; }
        public int RetainFileCount { get; set; }

        public LoggingConfig()
        {
            Level = "Info";
            LogDirectory = string.Empty;
            MaxFileBytes = 5 * 1024 * 1024;
            RetainFileCount = 5;
        }
    }

    [Serializable]
    public class DriverInstallConfig
    {
        /// <summary>
        /// Path to INF file if a default should be used.
        /// </summary>
        public string DefaultInfPath { get; set; }
        /// <summary>
        /// Allow test-signed drivers (development only).
        /// </summary>
        public bool AllowTestSignedDrivers { get; set; }
        /// <summary>
        /// Allow unsigned drivers (strongly discouraged).
        /// </summary>
        public bool AllowUnsignedDrivers { get; set; }

        public DriverInstallConfig()
        {
            DefaultInfPath = string.Empty;
            AllowTestSignedDrivers = false;
            AllowUnsignedDrivers = false;
        }
    }

    [Serializable]
    public class ServiceConfig
    {
        public bool RunAtStartup { get; set; }
        public bool AutoRestart { get; set; }
        public int RestartDelaySeconds { get; set; }
        public string ServiceAccount { get; set; }

        public ServiceConfig()
        {
            RunAtStartup = true;
            AutoRestart = true;
            RestartDelaySeconds = 5;
            ServiceAccount = "LocalSystem";
        }
    }

    #endregion
}
