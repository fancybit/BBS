using System;
using System.IO;
using BBSCLI.Logging;
using BBSLib;

namespace BBSCLI.Commands
{
    public class SelfCheckCommand : CommandBase
    {
        public SelfCheckCommand(Logger logger) : base("selfcheck", "Run self-check and report changed files", logger) { }

        public override int Execute(string[] args)
        {
            try
            {
                string baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
                string baseline = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : Path.Combine(baseDir, "file_hashes.txt");

                var checker = new SelfChecker(msg => Logger.Info(msg));

                // If baseline exists, verify and report mismatches
                if (File.Exists(baseline))
                {
                    Logger.Info("Verifying files against baseline: " + baseline);
                    var verifyResult = checker.VerifyHashesFromBaseline(baseline, baseDir);
                    Logger.Info($"Verify result: {verifyResult.Status} - {verifyResult.Message}");
                }
                else
                {
                    Logger.Info("Baseline not found. A new baseline will be created at: " + baseline);
                }

                // Always compute current hashes and write new baseline (overwrite)
                Logger.Info("Computing current file hashes and updating baseline...");
                var writeResult = checker.CheckFileHashes(baseDir, baseline);
                Logger.Info($"Hash generation result: {writeResult.Status} - {writeResult.Message}");

                // Run other checks and print short report
                var report = checker.RunAllChecks();
                Logger.Info("Self-check report:");
                foreach (var item in report.Items)
                {
                    Logger.Info($" - {item.Name}: {item.Status} - {item.Message}");
                }

                // If verify indicated mismatches previously, return non-zero
                if (File.Exists(baseline))
                {
                    var verifyAgain = checker.VerifyHashesFromBaseline(baseline, baseDir);
                    var msg = verifyAgain.Message ?? string.Empty;
                    if (msg.IndexOf("mismatch", System.StringComparison.OrdinalIgnoreCase) >= 0 || msg.IndexOf("mismatches", System.StringComparison.OrdinalIgnoreCase) >= 0 || msg.IndexOf("missing", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Logger.Warn("File integrity issues detected: " + verifyAgain.Message);
                        return 2;
                    }
                }

                return 0;
            }
            catch (System.Exception ex)
            {
                Logger.Error("Self-check failed: " + ex.Message);
                return 1;
            }
        }
    }
}
