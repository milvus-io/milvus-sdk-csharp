using IO.Milvus.ApiSchema;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using IO.Milvus.Diagnostics;
using System.Linq;
using IO.Milvus.Utils;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    ///<inheritdoc/>
    public async Task CreatePartitionAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Create partition {0}", collectionName);

        Grpc.CreatePartitionRequest request = CreatePartitionRequest
            .Create(collectionName, partitionName, dbName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.CreatePartitionAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Create partition failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task<bool> HasPartitionAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Check if partition {0} exists", collectionName);

        Grpc.HasPartitionRequest request = HasPartitionRequest
            .Create(collectionName, partitionName, dbName)
            .BuildGrpc();

        Grpc.BoolResponse response = await _grpcClient.HasPartitionAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Failed check if partition exists: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return response.Value;
    }

    ///<inheritdoc/>
    public async Task<IList<MilvusPartition>> ShowPartitionsAsync(
        string collectionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Show {0} collection partitions", collectionName);

        Grpc.ShowPartitionsRequest request = ShowPartitionsRequest
            .Create(collectionName, dbName)
            .BuildGrpc();

        Grpc.ShowPartitionsResponse response = await _grpcClient.ShowPartitionsAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Show partitions failed: {0}, {1}", response.Status.ErrorCode, response.Status.Reason);
            throw new MilvusException(response.Status);
        }

        return ToPartitions(response).ToList();
    }

    ///<inheritdoc/>
    public async Task LoadPartitionsAsync(
        string collectionName,
        IList<string> partitionNames,
        int replicaNumber = 1,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Load partitions {0}", collectionName);

        Grpc.LoadPartitionsRequest request = LoadPartitionsRequest
            .Create(collectionName, dbName)
            .WithPartitionNames(partitionNames)
            .WithReplicaNumber(replicaNumber)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.LoadPartitionsAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Load partitions failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task ReleasePartitionAsync(
        string collectionName,
        IList<string> partitionNames,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Release partitions {0}", collectionName);

        Grpc.ReleasePartitionsRequest request = ReleasePartitionRequest
            .Create(collectionName, dbName)
            .WithPartitionNames(partitionNames)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.ReleasePartitionsAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Release partitions failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    ///<inheritdoc/>
    public async Task DropPartitionsAsync(
        string collectionName,
        string partitionName,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Drop partition {0}", collectionName);

        Grpc.DropPartitionRequest request = DropPartitionRequest
            .Create(collectionName, partitionName, dbName)
            .BuildGrpc();

        Grpc.Status response = await _grpcClient.DropPartitionAsync(request, _callOptions.WithCancellationToken(cancellationToken));

        if (response.ErrorCode != Grpc.ErrorCode.Success)
        {
            _log.LogError("Drop partition failed: {0}, {1}", response.ErrorCode, response.Reason);
            throw new MilvusException(response);
        }
    }

    #region Private ================================================================
    private static IEnumerable<MilvusPartition> ToPartitions(Grpc.ShowPartitionsResponse response)
    {
        if (response.PartitionIDs == null)
            yield break;

        for (int i = 0; i < response.PartitionIDs.Count; i++)
        {
            yield return new MilvusPartition(
                response.PartitionIDs[i],
                response.PartitionNames[i],
                TimestampUtils.GetTimeFromTimstamp((long)response.CreatedUtcTimestamps[i]),
                response.InMemoryPercentages?.Count > 0 ? response.InMemoryPercentages[i] : -1);
        }
    }
    #endregion
}
