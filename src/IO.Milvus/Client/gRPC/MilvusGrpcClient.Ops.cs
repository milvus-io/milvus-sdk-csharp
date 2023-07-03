using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task<long> ManualCompactionAsync(
        long collectionId,
        DateTime? timeTravel = null,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(collectionId, 0);

        _log.LogDebug("Manual compaction {1}", collectionId);

        Grpc.ManualCompactionResponse response = await _grpcClient.ManualCompactionAsync(new Grpc.ManualCompactionRequest()
        {
            CollectionID = collectionId,
            Timetravel = timeTravel is not null ? (ulong)TimestampUtils.ToUtcTimestamp(timeTravel.Value) : 0,
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Manual compaction failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.CompactionID;
    }

    ///<inheritdoc/>
    public async Task<MilvusCompactionState> GetCompactionStateAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(compactionId, 0);

        _log.LogDebug("Get compaction state: {1}", compactionId);

        Grpc.GetCompactionStateResponse response = await _grpcClient.GetCompactionStateAsync(new Grpc.GetCompactionStateRequest()
        {
            CompactionID = compactionId,
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get compaction state: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return (MilvusCompactionState)response.State;
    }

    ///<inheritdoc/>
    public async Task<MilvusCompactionPlans> GetCompactionPlansAsync(
        long compactionId,
        CancellationToken cancellationToken = default)
    {
        Verify.GreaterThan(compactionId, 0); // TODO: The other's had this and this one didn't; was it intentional?

        _log.LogDebug("Get compaction plans: {1}", compactionId);

        Grpc.GetCompactionPlansResponse response = await _grpcClient.GetCompactionStateWithPlansAsync(new Grpc.GetCompactionPlansRequest()
        {
            CompactionID = compactionId
        }, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed get compaction plans: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return MilvusCompactionPlans.From(response);
    }
}
