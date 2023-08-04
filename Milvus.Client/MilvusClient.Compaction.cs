namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Gets the state of a compaction previously started via <see cref="MilvusCollection.CompactAsync" />.
    /// </summary>
    /// <param name="compactionId">The compaction ID returned by <see cref="MilvusCollection.CompactAsync" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<CompactionState> GetCompactionStateAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(compactionId, 0);

        GetCompactionStateResponse response = await InvokeAsync(GrpcClient.GetCompactionStateAsync,
                new GetCompactionStateRequest { CompactionID = compactionId }, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return (CompactionState)response.State;
    }

    /// <summary>
    /// Polls Milvus for the state of a compaction process until it is complete..
    /// To perform a single progress check, use <see cref="GetCompactionStateAsync" />.
    /// </summary>
    /// <param name="compactionId">The compaction ID returned by <see cref="MilvusCollection.CompactAsync" />.</param>
    /// <param name="waitingInterval">Waiting interval. Defaults to 500 milliseconds.</param>
    /// <param name="timeout">How long to poll for before throwing a <see cref="TimeoutException" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task WaitForCompactionAsync(
        long compactionId,
        TimeSpan? waitingInterval = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        await Utils.Poll(
            async () =>
            {
                CompactionState state = await GetCompactionStateAsync(compactionId, cancellationToken)
                    .ConfigureAwait(false);

                return state switch
                {
                    CompactionState.Undefined
                        => throw new InvalidOperationException($"Compaction with ID {compactionId} is in an undefined state."),
                    CompactionState.Executing => (false, 0),
                    CompactionState.Completed => (true, 0),

                    _ => throw new ArgumentOutOfRangeException("Invalid state: " + state)
                };
            },
            $"Timeout when waiting for compaction with ID {compactionId}",
            waitingInterval, timeout, progress: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the compaction states of a compaction.
    /// </summary>
    /// <param name="compactionId">The compaction ID returned by <see cref="MilvusCollection.CompactAsync" />.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<CompactionPlans> GetCompactionPlansAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(compactionId, 0);

        GetCompactionPlansResponse response = await InvokeAsync(GrpcClient.GetCompactionStateWithPlansAsync,
                new GetCompactionPlansRequest { CompactionID = compactionId }, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return new(
            response.MergeInfos
                .Select(static x => new MilvusCompactionPlan { Sources = x.Sources, Target = x.Target })
                .ToList(),
            (CompactionState)response.State);
    }
}
