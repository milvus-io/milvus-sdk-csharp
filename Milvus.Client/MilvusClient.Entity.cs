using System.Collections.Concurrent;

namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Maps collection names to their last known mutation timestamp.
    /// Used to implement <see cref="ConsistencyLevel.Session" />.
    /// </summary>
    internal ConcurrentDictionary<string, ulong> CollectionLastMutationTimestamps { get; } = new();

    /// <summary>
    /// Gets the flush state of multiple segments.
    /// </summary>
    /// <param name="segmentIds">A list of segment IDs for which to get the flush state</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>Whether the provided segments have been flushed.</returns>
    public async Task<bool> GetFlushStateAsync(
        IReadOnlyList<long> segmentIds,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(segmentIds);

        GetFlushStateRequest request = new();
        request.SegmentIDs.AddRange(segmentIds);

        GetFlushStateResponse response =
            await InvokeAsync(GrpcClient.GetFlushStateAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Flushed;
    }

    /// <summary>
    /// Flushes all collections. All insertions, deletions, and upserts prior to this call will be synced to disk.
    /// </summary>
    /// <remarks>
    /// While this method starts flushing, that process may not be complete when the method returns. The returned
    /// timestamp can be used to check on the state of the flush process, either via
    /// <see cref="GetFlushAllStateAsync" /> (for a single check) or via <see cref="WaitForFlushAllAsync" />
    /// (to wait until the process is complete).
    /// </remarks>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    /// A timestamp that can be passed to <see cref="GetFlushAllStateAsync" /> to check the state of the flush, or to
    /// <see cref="WaitForFlushAllAsync" /> to wait for it to complete.
    /// </returns>
    public async Task<ulong> FlushAllAsync(CancellationToken cancellationToken = default)
    {
        FlushAllResponse response =
            await InvokeAsync(GrpcClient.FlushAllAsync, new FlushAllRequest(), static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.FlushAllTs;
    }

    /// <summary>
    /// Returns whether a flush initiated by a previous <see cref="FlushAllAsync(CancellationToken)" /> call has
    /// completed.
    /// </summary>
    /// <param name="timestamp">A timestamp value return by <see cref="FlushAllAsync(CancellationToken)" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<bool> GetFlushAllStateAsync(
        ulong timestamp,
        CancellationToken cancellationToken = default)
    {
        GetFlushAllStateRequest request = new() { FlushAllTs = timestamp };

        GetFlushAllStateResponse response =
            await InvokeAsync(GrpcClient.GetFlushAllStateAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Flushed;
    }

    /// <summary>
    /// Polls Milvus for the state of a flush process initiated by a previous
    /// <see cref="FlushAllAsync(CancellationToken)" /> call, until that process is complete.
    /// </summary>
    /// <param name="timestamp">A timestamp value return by <see cref="FlushAllAsync(CancellationToken)" />.</param>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">How long to poll for before throwing a <see cref="TimeoutException" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task WaitForFlushAllAsync(
        ulong timestamp,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        await Utils.Poll(
            async () =>
            {
                bool result = await GetFlushAllStateAsync(timestamp, cancellationToken)
                    .ConfigureAwait(false);
                return (result, result);
            },
            $"Timeout when waiting for flush all",
            waitingInterval, timeout, default, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Polls Milvus for the flush state of the specific segments, until those segments are fully flushed.
    /// </summary>
    /// <param name="segmentIds">The segment IDs whose flush state should be polled..</param>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">How long to poll for before throwing a <see cref="TimeoutException" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task WaitForFlushAsync(
        IReadOnlyList<long> segmentIds,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(segmentIds);

        await Utils.Poll(
            async () =>
            {
                bool flushState = await GetFlushStateAsync(segmentIds, cancellationToken).ConfigureAwait(false);
                return (flushState, flushState);
            },
            $"Timeout when waiting for flush specified segments",
            waitingInterval, timeout, default, cancellationToken).ConfigureAwait(false);
    }
}
