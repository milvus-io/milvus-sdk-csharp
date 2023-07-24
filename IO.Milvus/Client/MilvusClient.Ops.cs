using IO.Milvus.Grpc;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Do a manual compaction.
    /// </summary>
    /// <param name="collectionId">Collection Id.</param>
    /// <param name="timeTravelTimestamp">Time travel.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>CompactionId</returns>
    public async Task<long> ManualCompactionAsync(
        long collectionId,
        ulong timeTravelTimestamp = 0,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(collectionId, 0);

        ManualCompactionResponse response = await InvokeAsync(GrpcClient.ManualCompactionAsync, new ManualCompactionRequest
        {
            CollectionID = collectionId,
            Timetravel = timeTravelTimestamp
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.CompactionID;
    }

    /// <summary>
    /// Get the state of a compaction
    /// </summary>
    /// <param name="compactionId">Collection id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<MilvusCompactionState> GetCompactionStateAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(compactionId, 0);

        GetCompactionStateResponse response = await InvokeAsync(GrpcClient.GetCompactionStateAsync, new GetCompactionStateRequest
        {
            CompactionID = compactionId
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return (MilvusCompactionState)response.State;
    }

    /// <summary>
    /// Get the plans of a compaction.
    /// </summary>
    /// <param name="compactionId">Compaction id.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<MilvusCompactionPlans> GetCompactionPlansAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(compactionId, 0);

        GetCompactionPlansResponse response = await InvokeAsync(GrpcClient.GetCompactionStateWithPlansAsync, new GetCompactionPlansRequest
        {
            CompactionID = compactionId
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusCompactionPlans.From(response);
    }
}
