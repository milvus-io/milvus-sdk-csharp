using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Do a manual compaction.
    /// </summary>
    /// <param name="collectionId">Collection Id.</param>
    /// <param name="timeTravel">Time travel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CompactionId</returns>
    public async Task<long> ManualCompactionAsync(
        long collectionId,
        DateTime? timeTravel = null,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(collectionId, 0);

        ManualCompactionResponse response = await InvokeAsync(_grpcClient.ManualCompactionAsync, new ManualCompactionRequest
        {
            CollectionID = collectionId,
            Timetravel = timeTravel is not null ? (ulong)timeTravel.Value.ToUtcTimestamp() : 0
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

        GetCompactionStateResponse response = await InvokeAsync(_grpcClient.GetCompactionStateAsync, new GetCompactionStateRequest
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

        GetCompactionPlansResponse response = await InvokeAsync(_grpcClient.GetCompactionStateWithPlansAsync, new GetCompactionPlansRequest
        {
            CompactionID = compactionId
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return MilvusCompactionPlans.From(response);
    }
}
