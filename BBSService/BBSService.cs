using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BBSService.Tasks;

namespace BBSService
{
    public partial class BBSService : ServiceBase
    {
        private readonly List<Task> _runningTasks = new List<Task>();
        private CancellationTokenSource _cts;

        public BBSService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // Configure tasks (intervals can be adjusted or moved to config)
            var tasks = new ITask[]
            {
                new SelfCheckTask(TimeSpan.FromHours(6)),
                new UpdateCheckTask("", TimeSpan.FromHours(12)), // TODO configure endpoint
                new UserDataBackupTask(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups"),
                    TimeSpan.FromDays(1))
            };

            foreach (var t in tasks)
            {
                var run = Task.Run(() => t.RunAsync(ct), ct);
                _runningTasks.Add(run);
            }
        }

        protected override void OnStop()
        {
            try
            {
                _cts?.Cancel();
                Task.WaitAll(_runningTasks.ToArray(), TimeSpan.FromSeconds(10));
            }
            catch { }
            finally
            {
                _cts?.Dispose();
                _runningTasks.Clear();
            }
        }
    }
}
