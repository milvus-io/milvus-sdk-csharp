using IO.MilvusTests.Client;
using System.Diagnostics;
using IO.Milvus.Client;

namespace IO.Milvus;

/// <summary>
/// Extension methods for <see cref="Client.MilvusClient"/>
/// </summary>
public static class MilvusClientExtensions
{
    /// <summary>
    /// Polls Milvus for loading progress of a collection until it is fully loaded.
    /// To perform a single progress check, use <see cref="MilvusClient.GetLoadingProgressAsync" />.
    /// </summary>
    /// <param name="milvusClient">Milvus client.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">Timeout.</param>
    /// <param name="progress">Provides information about the progress of the loading operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <exception cref="TimeoutException">Time out.</exception>
    public static async Task WaitForCollectionLoadAsync(
        this MilvusClient milvusClient,
        string collectionName,
        IList<string>? partitionNames = null,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(milvusClient);

        partitionNames ??= Array.Empty<string>();

        await Poll(
            async () =>
            {
                long progress = await milvusClient
                    .GetLoadingProgressAsync(collectionName, partitionNames, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return (progress == 100, progress);
            },
            $"Timeout when waiting for collection '{collectionName}' to load",
            waitingInterval, timeout, progress, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Polls Milvus for building progress of an index until it is fully built.
    /// To perform a single progress check, use <see cref="MilvusClient.GetIndexBuildProgressAsync" />.
    /// </summary>
    /// <param name="milvusClient">Milvus client.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="fieldName">The vector field name in this particular collection</param>
    /// <param name="indexName">An optional name for the index to be created.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">Timeout.</param>
    /// <param name="progress">Provides information about the progress of the loading operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <exception cref="TimeoutException">Time out.</exception>
    public static async Task WaitForIndexBuildAsync(
        this MilvusClient milvusClient,
        string collectionName,
        string fieldName,
        string? indexName = null,
        string? dbName = null,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        IProgress<IndexBuildProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(milvusClient);

        await Poll(
            async () =>
            {
                IndexBuildProgress progress = await milvusClient
                    .GetIndexBuildProgressAsync(collectionName, fieldName, dbName, indexName, cancellationToken)
                    .ConfigureAwait(false);
                return (progress.IsComplete, progress);
            },
            $"Timeout when waiting for index '{collectionName}' to build",
            waitingInterval, timeout, progress, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Wrapper methods for <see cref="MilvusClient.GetVersionAsync(CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Return <see cref="MilvusVersion"/> instead of <see cref="string"/>.
    /// </remarks>
    /// <param name="milvusClient"></param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>Milvus version</returns>
    public static async Task<MilvusVersion> GetMilvusVersionAsync(
        this MilvusClient milvusClient,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(milvusClient);

        string version = await milvusClient.GetVersionAsync(cancellationToken).ConfigureAwait(false);
        return MilvusVersion.Parse(version);
    }

    private static async Task Poll<TProgress>(
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
