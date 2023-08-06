using System.Diagnostics;

namespace Milvus.Client;

internal static class Utils
{
    internal static async Task Poll<TProgress>(
        Func<Task<(bool, TProgress)>> pollingAction,
        string timeoutExceptionMessage,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        IProgress<TProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        waitingInterval ??= TimeSpan.FromMilliseconds(500);

        Stopwatch? stopWatch = timeout is null ? null : Stopwatch.StartNew();

        while (true)
        {
            (bool isComplete, TProgress currentProgress) = await pollingAction().ConfigureAwait(false);

            progress?.Report(currentProgress);

            if (isComplete)
            {
                return;
            }

            if (stopWatch is not null && stopWatch.Elapsed + waitingInterval.Value >= timeout)
            {
                throw new TimeoutException(timeoutExceptionMessage);
            }

            await Task.Delay(waitingInterval.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
