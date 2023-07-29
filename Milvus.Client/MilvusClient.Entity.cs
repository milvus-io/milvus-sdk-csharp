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
    /// Get the flush state of multiple segments.
    /// </summary>
    /// <param name="segmentIds">Segment ids</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>If segments flushed.</returns>
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
    /// Flush all collections. All insertions, deletions, and upserts before <see cref="FlushAllAsync(CancellationToken)"/> will be synced.
    /// </summary>
    /// <remarks>
    /// This method returns FlushAllTs, but the returned FlushAllTs may not yet be persisted.
    /// If <see cref="GetFlushAllStateAsync(ulong, CancellationToken)"/> returns True, then we can say that the flushAll action is finished.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>FlushAllTs</returns>
    public async Task<ulong> FlushAllAsync(CancellationToken cancellationToken = default)
    {
        FlushAllResponse response =
            await InvokeAsync(GrpcClient.FlushAllAsync, new FlushAllRequest(), static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.FlushAllTs;
    }

    /// <summary>
    /// Get the flush state of <see cref="FlushAllAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="flushAllTs">Value return by <see cref="FlushAllAsync(CancellationToken)"/></param>
    /// <param name="cancellationToken"></param>
    public async Task<bool> GetFlushAllStateAsync(
        ulong flushAllTs,
        CancellationToken cancellationToken = default)
    {
        GetFlushAllStateRequest request = new() { FlushAllTs = flushAllTs };

        GetFlushAllStateResponse response =
            await InvokeAsync(GrpcClient.GetFlushAllStateAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Flushed;
    }

    /// <summary>
    /// Polls Milvus for the flush all state.
    /// </summary>
    /// <param name="flushAllTs">Flush all timestamp.</param>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">How long to poll for before throwing a <see cref="TimeoutException" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task WaitForFlushAllAsync(
        ulong flushAllTs,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        await Utils.Poll(
            async () =>
            {
                bool result = await GetFlushAllStateAsync(flushAllTs, cancellationToken)
                    .ConfigureAwait(false);
                return (result, result);
            },
            $"Timeout when waiting for flush all",
            waitingInterval, timeout, default, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Polls Milvus for the flush state of the specified segments.
    /// </summary>
    /// <param name="segmentIds">Segment Ids.</param>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">How long to poll for before throwing a <see cref="TimeoutException" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task WaitForFlushAsync(
        IReadOnlyList<long> segmentIds,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
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
