using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Create partition {0}", collectionName);

        Grpc.Status response = await _grpcClient.CreatePartitionAsync(new Grpc.CreatePartitionRequest()
        {
            CollectionName = collectionName,
            PartitionName = partitionName,
            DbName = dbName,
        }, _callOptions.WithCancellationToken(cancellationToken));

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Check if partition {0} exists", collectionName);

        Grpc.BoolResponse response = await _grpcClient.HasPartitionAsync(new Grpc.HasPartitionRequest()
        {
            CollectionName = collectionName,
            PartitionName = partitionName,
            DbName = dbName,
        }, _callOptions.WithCancellationToken(cancellationToken));

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Show {0} collection partitions", collectionName);

        Grpc.ShowPartitionsResponse response = await _grpcClient.ShowPartitionsAsync(new Grpc.ShowPartitionsRequest()
        {
            CollectionName = collectionName,
            DbName = dbName
        }, _callOptions.WithCancellationToken(cancellationToken));

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

        Grpc.LoadPartitionsRequest request = new Grpc.LoadPartitionsRequest()
        {
            CollectionName = collectionName,
            ReplicaNumber = replicaNumber,
            DbName = dbName
        };
        request.PartitionNames.AddRange(partitionNames);

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Release partitions {0}", collectionName);

        Grpc.ReleasePartitionsRequest request = new Grpc.ReleasePartitionsRequest()
        {
            CollectionName = collectionName,
            DbName = dbName
        };
        request.PartitionNames.AddRange(partitionNames);

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
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        _log.LogDebug("Drop partition {0}", collectionName);

        Grpc.Status response = await _grpcClient.DropPartitionAsync(new Grpc.DropPartitionRequest()
        {
            CollectionName = collectionName,
            PartitionName = partitionName
        }, _callOptions.WithCancellationToken(cancellationToken));

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
