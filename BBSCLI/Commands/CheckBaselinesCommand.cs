using System;
using System.Diagnostics;
using System.IO;
using BBSCLI.Logging;

namespace BBSCLI.Commands
{
 public class CheckBaselinesCommand : CommandBase
 {
 public CheckBaselinesCommand(Logger logger) : base("checkbaselines", "Open the selfcheck baselines folder", logger) { }

 public override int Execute(string[] args)
 {
 try
 {
 var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
 var logDir = Path.Combine(baseDir, "selfchecklog");
 if (!Directory.Exists(logDir))
 {
 Logger.Warn("Baselines folder not found; creating: " + logDir);
 Directory.CreateDirectory(logDir);
 }

 Logger.Info("Opening baselines folder: " + logDir);

 try
 {
 // Use shell execute to open folder in explorer on Windows; works cross-platform in .NET for opening folder
 var psi = new ProcessStartInfo
 {
 FileName = logDir,
 UseShellExecute = true
 };
 Process.Start(psi);
 return0;
 }
 catch (Exception ex)
 {
 Logger.Error("Failed to open baselines folder: " + ex.Message);
 return2;
 }
 }
 catch (Exception ex)
 {
 Logger.Error("CheckBaselines failed: " + ex.Message);
 return1;
 }
 }
 }
}
