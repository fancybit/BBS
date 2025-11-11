using System;
using System.Threading;
using System.Threading.Tasks;

namespace BBSService.Tasks
{
    public abstract class ServiceTaskBase : ITask
    {
        public string Name { get; }
        protected ServiceTaskBase(string name)
        {
            Name = name;
        }

        protected virtual void Log(string message)
        {
            try { System.Diagnostics.EventLog.WriteEntry("BBSService", $"[{Name}] {message}", System.Diagnostics.EventLogEntryType.Information); }
            catch { }
        }

        protected virtual void LogError(string message)
        {
            try { System.Diagnostics.EventLog.WriteEntry("BBSService", $"[{Name}] ERROR: {message}", System.Diagnostics.EventLogEntryType.Error); }
            catch { }
        }

        public abstract Task RunAsync(CancellationToken ct);
    }
}
