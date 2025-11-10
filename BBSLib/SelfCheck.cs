using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Security.Cryptography;
using System.Text;

namespace BBSLib
{
    /// <summary>
    /// Status for an individual self-check item.
    /// </summary>
    public enum CheckStatus
    {
        Ok,
        Warning,
        Error
    }

    /// <summary>
    /// Result for a single check.
    /// </summary>
    public class SelfCheckItem
    {
        public string Name { get; set; }
        public CheckStatus Status { get; set; }
        public string Message { get; set; }

        public SelfCheckItem(string name, CheckStatus status, string message = null)
        {
            Name = name ?? string.Empty;
            Status = status;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>
    /// Aggregate report for self-check run.
    /// </summary>
    public class SelfCheckReport
    {
        public List<SelfCheckItem> Items { get; } = new List<SelfCheckItem>();

        public bool IsSuccess
        {
            get { return Items.All(i => i.Status != CheckStatus.Error); }
        }

        public void Add(SelfCheckItem item)
        {
            Items.Add(item);
        }
    }

    /// <summary>
    /// Performs a series of application-level self checks (disk, permissions, files, services, tools).
    /// </summary>
    public class SelfChecker
    {
        private readonly Action<string> _logger;

        public SelfChecker(Action<string> logger = null)
        {
            _logger = logger ?? (_ => { });
        }

        /// <summary>
        /// Run default checks and return a report.
        /// Defaults: admin rights, free disk space (100 MB), pnputil availability, presence of BBSDrv.inf in expected locations, service existence (BBSService), compute file hashes baseline.
        /// </summary>
        public SelfCheckReport RunAllChecks()
        {
            var report = new SelfCheckReport();

            report.Add(CheckIsElevated());
            report.Add(CheckPnputil());
            report.Add(CheckDiskSpace(100 * 1024 * 1024)); //100 MB
            report.Add(CheckInfFile());
            report.Add(CheckServiceExists("BBSService"));

            // compute file hashes baseline and write timestamped baseline into selfchecklog folder
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logDir = Path.Combine(baseDir, "selfchecklog");
                report.Add(SaveBaselineWithChanges(baseDir, logDir));
            }
            catch (Exception ex)
            {
                report.Add(new SelfCheckItem("File hashes", CheckStatus.Warning, "Failed to compute file hashes: " + ex.Message));
            }

            _logger("Self-check complete. Items: " + report.Items.Count);
            return report;
        }

        /// <summary>
        /// Check if process is elevated (running as Administrator).
        /// </summary>
        public SelfCheckItem CheckIsElevated()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                return new SelfCheckItem("Administrator privileges", isAdmin ? CheckStatus.Ok : CheckStatus.Warning,
                isAdmin ? "Running as administrator." : "Not running as administrator. Some operations may fail.");
            }
            catch (Exception ex)
            {
                return new SelfCheckItem("Administrator privileges", CheckStatus.Error, "Failed to determine elevation: " + ex.Message);
            }
        }

        /// <summary>
        /// Check available disk space on the drive where the entry assembly lives.
        /// </summary>
        public SelfCheckItem CheckDiskSpace(long minimumBytes)
        {
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var codeBase = asm.Location;
                if (string.IsNullOrEmpty(codeBase))
                {
                    return new SelfCheckItem("Disk space", CheckStatus.Warning, "Cannot determine assembly location.");
                }

                var root = Path.GetPathRoot(codeBase);
                if (string.IsNullOrEmpty(root))
                    return new SelfCheckItem("Disk space", CheckStatus.Warning, "Cannot determine drive root.");

                var drive = new DriveInfo(root);
                var free = drive.AvailableFreeSpace;
                var ok = free >= minimumBytes;
                return new SelfCheckItem("Disk space", ok ? CheckStatus.Ok : CheckStatus.Error,
                string.Format("Available: {0} MB; Required: {1} MB", (free / 1024 / 1024), (minimumBytes / 1024 / 1024)));
            }
            catch (Exception ex)
            {
                return new SelfCheckItem("Disk space", CheckStatus.Error, "Failed to check disk space: " + ex.Message);
            }
        }

        /// <summary>
        /// Check presence of expected INF file for BBSDrv in common locations.
        /// </summary>
        public SelfCheckItem CheckInfFile()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidates = new[]
                {
 Path.Combine(baseDir, "BBSDrv", "BBSDrv.inf"),
 Path.Combine(baseDir, "..", "BBSDrv", "BBSDrv.inf"),
 Path.Combine(baseDir, "..", "..", "BBSDrv", "BBSDrv.inf")
 };
                var found = candidates.FirstOrDefault(File.Exists);
                if (found != null)
                    return new SelfCheckItem("BBSDrv INF", CheckStatus.Ok, "Found: " + Path.GetFullPath(found));
                return new SelfCheckItem("BBSDrv INF", CheckStatus.Warning, "BBSDrv.inf not found in expected locations.");
            }
            catch (Exception ex)
            {
                return new SelfCheckItem("BBSDrv INF", CheckStatus.Error, "Failed to check INF file: " + ex.Message);
            }
        }

        /// <summary>
        /// Check whether a Windows service with the specified name exists.
        /// </summary>
        public SelfCheckItem CheckServiceExists(string serviceName)
        {
            try
            {
                if (string.IsNullOrEmpty(serviceName))
                    return new SelfCheckItem("Service check", CheckStatus.Warning, "No service name provided.");

                try
                {
                    var svc = ServiceController.GetServices().FirstOrDefault(s => string.Equals(s.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase) || string.Equals(s.DisplayName, serviceName, StringComparison.OrdinalIgnoreCase));
                    if (svc != null)
                        return new SelfCheckItem("Service: " + serviceName, CheckStatus.Ok, "Service exists. Status: " + svc.Status);
                    return new SelfCheckItem("Service: " + serviceName, CheckStatus.Warning, "Service not found.");
                }
                catch (PlatformNotSupportedException)
                {
                    return new SelfCheckItem("Service: " + serviceName, CheckStatus.Warning, "Service APIs not supported on this platform.");
                }
            }
            catch (Exception ex)
            {
                return new SelfCheckItem("Service: " + serviceName, CheckStatus.Error, "Failed to check service: " + ex.Message);
            }
        }

        /// <summary>
        /// Check whether pnputil.exe is available under System32 or in PATH.
        /// </summary>
        public SelfCheckItem CheckPnputil()
        {
            try
            {
                var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var sysPath = Path.Combine(system, "pnputil.exe");
                if (File.Exists(sysPath))
                    return new SelfCheckItem("pnputil", CheckStatus.Ok, "Found in System folder.");

                // Try PATH
                var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator);
                foreach (var p in paths)
                {
                    try
                    {
                        var candidate = Path.Combine(p, "pnputil.exe");
                        if (File.Exists(candidate))
                            return new SelfCheckItem("pnputil", CheckStatus.Ok, "Found in PATH: " + candidate);
                    }
                    catch { }
                }

                return new SelfCheckItem("pnputil", CheckStatus.Warning, "pnputil.exe not found. Driver install may fail.");
            }
            catch (Exception ex)
            {
                return new SelfCheckItem("pnputil", CheckStatus.Error, "Failed to check pnputil: " + ex.Message);
            }
        }

        /// <summary>
        /// Compute MD5 hashes for files matching patterns under rootDir. Returns dictionary keyed by relative path.
        /// </summary>
        public Dictionary<string, string> ComputeFileHashes(string rootDir, string[] patterns = null)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(rootDir)) rootDir = AppDomain.CurrentDomain.BaseDirectory;
            if (patterns == null || patterns.Length == 0)
            {
                patterns = new[] { "*.dll", "*.exe", "*.cs" };
            }

            try
            {
                foreach (var pattern in patterns)
                {
                    string[] files = Directory.GetFiles(rootDir, pattern, SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            string rel = MakeRelativePath(rootDir, file);
                            string hash = ComputeMd5(file);
                            if (!result.ContainsKey(rel))
                                result.Add(rel, hash);
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Compute MD5 and write baseline to outputFile (if provided). Returns a SelfCheckItem summarizing the result.
        /// </summary>
        public SelfCheckItem CheckFileHashes(string rootDir, string outputFile = null)
        {
            try
            {
                var map = ComputeFileHashes(rootDir, null);
                if (map == null || map.Count == 0)
                {
                    return new SelfCheckItem("File hashes", CheckStatus.Warning, "No files found to hash.");
                }

                if (!string.IsNullOrEmpty(outputFile))
                {
                    try
                    {
                        var lines = new List<string>();
                        foreach (var kv in map.OrderBy(k => k.Key))
                        {
                            lines.Add(kv.Key + " " + kv.Value);
                        }
                        File.WriteAllLines(outputFile, lines, Encoding.UTF8);
                        return new SelfCheckItem("File hashes", CheckStatus.Ok, string.Format("Computed {0} hashes and saved to {1}", map.Count, outputFile));
                    }
                    catch (Exception ex)
                    {
                        return new SelfCheckItem("File hashes", CheckStatus.Warning, "Computed hashes but failed to write baseline: " + ex.Message);
                    }
                }

                return new SelfCheckItem("File hashes", CheckStatus.Ok, string.Format("Computed {0} hashes.", map.Count));
            }
            catch (Exception ex)
            {
                return new SelfCheckItem("File hashes", CheckStatus.Error, "Failed to compute file hashes: " + ex.Message);
            }
        }

        /// <summary>
        /// Verify current file hashes against a baseline file. Baseline format: "relativePath md5" per line.
        /// Returns a SelfCheckItem indicating mismatches or success.
        /// </summary>
        public SelfCheckItem VerifyHashesFromBaseline(string baselineFile, string rootDir)
        {
            try
            {
                if (string.IsNullOrEmpty(baselineFile) || !File.Exists(baselineFile))
                    return new SelfCheckItem("Verify hashes", CheckStatus.Warning, "Baseline file not found: " + baselineFile);

                var lines = File.ReadAllLines(baselineFile, Encoding.UTF8);
                var baseline = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var idx = line.IndexOf(' ');
                    if (idx <= 0) continue;
                    var rel = line.Substring(0, idx).Trim();
                    var hash = line.Substring(idx + 1).Trim();
                    baseline[rel] = hash;
                }

                var current = ComputeFileHashes(rootDir, baseline.Keys.Select(k => Path.GetExtension(k)).Distinct().Select(ext => "*" + (ext.StartsWith(".") ? ext : ext)).ToArray());

                var mismatches = new List<string>();
                foreach (var kv in baseline)
                {
                    string key = kv.Key;
                    string expected = kv.Value;
                    string actual;
                    if (!current.TryGetValue(key, out actual))
                    {
                        mismatches.Add(key + " (missing)");
                    }
                    else if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                    {
                        mismatches.Add(key + " (mismatch)");
                    }
                }

                if (mismatches.Count == 0)
                {
                    return new SelfCheckItem("Verify hashes", CheckStatus.Ok, "All files match baseline.");
                }
                else
                {
                    return new SelfCheckItem("Verify hashes", CheckStatus.Error, "Mismatches: " + string.Join(", ", mismatches.Take(20)) + (mismatches.Count > 20 ? " ..." : string.Empty));
                }
            }
            catch (Exception ex)
            {
                return new SelfCheckItem("Verify hashes", CheckStatus.Error, "Failed to verify hashes: " + ex.Message);
            }
        }

        private static string ComputeMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    var sb = new StringBuilder(hash.Length * 2);
                    for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                    return sb.ToString();
                }
            }
        }

        private static string MakeRelativePath(string rootPath, string fullPath)
        {
            try
            {
                var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var full = Path.GetFullPath(fullPath);
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return full.Substring(root.Length);
                }
                return full;
            }
            catch
            {
                return fullPath;
            }
        }

        /// <summary>
        /// Compute current hashes, compare with latest baseline in logDir (if any), write a new timestamped baseline file into logDir and append changed file list.
        /// Returns a SelfCheckItem summarizing the operation.
        /// </summary>
        public SelfCheckItem SaveBaselineWithChanges(string rootDir, string logDir)
        {
            try
            {
                if (string.IsNullOrEmpty(rootDir)) rootDir = AppDomain.CurrentDomain.BaseDirectory;
                if (string.IsNullOrEmpty(logDir)) logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selfchecklog");
                Directory.CreateDirectory(logDir);

                // compute current map
                var current = ComputeFileHashes(rootDir, null);

                // find latest baseline file in logDir (by name or last write)
                var files = Directory.GetFiles(logDir, "baseline_*.txt", SearchOption.TopDirectoryOnly);
                string latest = null;
                DateTime latestTime = DateTime.MinValue;
                foreach (var f in files)
                {
                    try
                    {
                        var dt = File.GetLastWriteTimeUtc(f);
                        if (dt > latestTime) { latestTime = dt; latest = f; }
                    }
                    catch { }
                }

                Dictionary<string, string> previous = null;
                if (!string.IsNullOrEmpty(latest) && File.Exists(latest))
                {
                    try
                    {
                        var lines = File.ReadAllLines(latest, Encoding.UTF8);
                        previous = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var idx = line.IndexOf(' ');
                            if (idx <=0) continue;
                            var rel = line.Substring(0, idx).Trim();
                            var hash = line.Substring(idx +1).Trim();
                            previous[rel] = hash;
                        }
                    }
                    catch { previous = null; }
                }

                // detect changes
                var changes = new List<string>();
                if (previous != null)
                {
                    // missing or mismatched
                    foreach (var kv in previous)
                    {
                        string rel = kv.Key;
                        string prevHash = kv.Value;
                        string curHash;
                        if (!current.TryGetValue(rel, out curHash))
                        {
                            changes.Add(rel + " (missing)");
                        }
                        else if (!string.Equals(prevHash, curHash, StringComparison.OrdinalIgnoreCase))
                        {
                            changes.Add(rel + " (mismatch)");
                        }
                    }
                    // new files
                    foreach (var kv in current)
                    {
                        if (!previous.ContainsKey(kv.Key))
                        {
                            changes.Add(kv.Key + " (new)");
                        }
                    }
                }

                // write new baseline
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var outFile = Path.Combine(logDir, string.Format("baseline_{0}.txt", timestamp));
                var linesOut = new List<string>();
                foreach (var kv in current.OrderBy(k => k.Key))
                {
                    linesOut.Add(kv.Key + " " + kv.Value);
                }
                // append separator and changes summary
                if (changes.Count >0)
                {
                    linesOut.Add(string.Empty);
                    linesOut.Add("# Changes since previous baseline:");
                    foreach (var c in changes)
                        linesOut.Add(c);
                }
                File.WriteAllLines(outFile, linesOut, Encoding.UTF8);

                var msg = string.Format("Wrote baseline {0} with {1} files", Path.GetFileName(outFile), current.Count);
                if (changes.Count >0)
                    msg += "; changes: " + string.Join(", ", changes.Take(20)) + (changes.Count >20 ? " ..." : string.Empty);

                return new SelfCheckItem("File hashes", CheckStatus.Ok, msg);
            }
            catch (Exception ex)
            {
                return new SelfCheckItem("File hashes", CheckStatus.Error, "Failed to save baseline: " + ex.Message);
            }
        }
    }
}
