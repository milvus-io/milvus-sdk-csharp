using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    /// <inheritdoc />
    public async Task<long> ManualCompactionAsync(
        long collectionId,
        DateTime? timeTravel = null,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(collectionId, 0);

        ManualCompactionResponse response = await InvokeAsync(_grpcClient.ManualCompactionAsync, new ManualCompactionRequest
        {
            CollectionID = collectionId,
            Timetravel = timeTravel is not null ? (ulong)TimestampUtils.ToUtcTimestamp(timeTravel.Value) : 0,
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.CompactionID;
    }

    /// <inheritdoc />
    public async Task<MilvusCompactionState> GetCompactionStateAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(compactionId, 0);

        GetCompactionStateResponse response = await InvokeAsync(_grpcClient.GetCompactionStateAsync, new GetCompactionStateRequest
        {
            CompactionID = compactionId,
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

        return (MilvusCompactionState)response.State;
    }

    /// <inheritdoc />
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
