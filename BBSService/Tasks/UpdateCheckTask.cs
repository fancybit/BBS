using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BBSService.Tasks
{
    /// <summary>
    /// Checks for updates by calling a configured endpoint.
    /// </summary>
    public sealed class UpdateCheckTask : ServiceTaskBase
    {
        private readonly TimeSpan _interval;
        private readonly string _endpoint;
        private readonly HttpClient _http;
        public UpdateCheckTask(string endpoint, TimeSpan interval) : base("UpdateCheck")
        {
            _endpoint = endpoint ?? string.Empty;
            _interval = interval;
            _http = new HttpClient();
        }

        public override async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_endpoint))
                    {
                        var resp = await _http.GetAsync(_endpoint, ct).ConfigureAwait(false);
                        Log($"HTTP {(int)resp.StatusCode} {_endpoint}");
                    }
                    else
                    {
                        Log("No update endpoint configured.");
                    }
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
