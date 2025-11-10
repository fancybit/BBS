using System;
using System.IO;

namespace BBSCLI.Logging
{
    public class Logger
    {
        private readonly object _lock = new object();
        private readonly string? _logFilePath;

        public Logger(string? logFilePath = null)
        {
            _logFilePath = logFilePath;
        }

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Warn(string message)
        {
            Write("WARN", message);
        }

        public void Error(string message)
        {
            Write("ERROR", message);
        }

        private void Write(string level, string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level}: {message}";
            lock (_lock)
            {
                Console.WriteLine(line);
                if (!string.IsNullOrEmpty(_logFilePath))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath) ?? ".");
                        File.AppendAllText(_logFilePath, line + Environment.NewLine);
                    }
                    catch
                    {
                        // swallow logging errors
                    }
                }
            }
        }
    }
}
