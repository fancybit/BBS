using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BBSLib;

namespace BBSService.Tasks
{
    /// <summary>
    /// Periodically runs self-check and writes a timestamped baseline into selfchecklog.
    /// </summary>
    public sealed class SelfCheckTask : ServiceTaskBase
    {
        private readonly TimeSpan _interval;
        public SelfCheckTask(TimeSpan interval) : base("SelfCheck")
        {
            _interval = interval;
        }

        public override async Task RunAsync(CancellationToken ct)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var logDir = Path.Combine(baseDir, "selfchecklog");
            var checker = new SelfChecker(msg => Log(msg));
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var item = checker.SaveBaselineWithChanges(baseDir, logDir);
                    Log($"{item.Status}: {item.Message}");
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
