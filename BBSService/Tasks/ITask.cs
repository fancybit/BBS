using System.Threading;
using System.Threading.Tasks;

namespace BBSService.Tasks
{
    /// <summary>
    /// Basic task contract for background jobs inside the service.
    /// </summary>
    public interface ITask
    {
        string Name { get; }
        /// <summary>
        /// Execute the task once.
        /// </summary>
        Task RunAsync(CancellationToken ct);
    }
}
