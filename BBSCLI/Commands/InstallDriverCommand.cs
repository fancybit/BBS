using System;
using System.IO;
using BBSCLI.Logging;

namespace BBSCLI.Commands
{
    public class InstallDriverCommand : CommandBase
    {
        public InstallDriverCommand(Logger logger) : base("install-driver", "Install the BBS driver (inf)", logger) { }

        public override int Execute(string[] args)
        {
            string? infPath = args.Length > 0 ? args[0] : null;
            if (string.IsNullOrWhiteSpace(infPath))
            {
                // try discover
                var baseDir = AppContext.BaseDirectory;
                infPath = Path.Combine(baseDir, "BBSDrv", "BBSDrv.inf");
            }
            Logger.Info($"Installing driver from: {infPath}");
            if (!File.Exists(infPath))
            {
                Logger.Error($"INF not found: {infPath}");
                return 2;
            }
            // require elevation
            // try run pnputil
            var psi = new System.Diagnostics.ProcessStartInfo("pnputil.exe")
            {
                Arguments = $"/add-driver \"{infPath}\" /install",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(infPath)
            };
            try
            {
                var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null)
                {
                    Logger.Error("Failed to start pnputil");
                    return 4;
                }
                proc.WaitForExit();
                Logger.Info($"pnputil exit code: {proc.ExitCode}");
                return proc.ExitCode;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to run pnputil: " + ex.Message);
                return 5;
            }
        }
    }
}
