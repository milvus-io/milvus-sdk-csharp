using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Create a partition.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to create.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CreatePartitionAsync(
        string collectionName,
        string partitionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);

        var request = new CreatePartitionRequest { CollectionName = collectionName, PartitionName = partitionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.CreatePartitionAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///  Get if a partition exists.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to check.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<bool> HasPartitionAsync(
        string collectionName,
        string partitionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);

        var request = new HasPartitionRequest { CollectionName = collectionName, PartitionName = partitionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        BoolResponse response =
            await InvokeAsync(_grpcClient.HasPartitionAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Value;
    }

    /// <summary>
    /// Show all partitions.
    /// </summary>
    /// <param name="collectionName">The collection name you want to describe,
    /// you can pass collection_name or collectionID.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task<IList<MilvusPartition>> ShowPartitionsAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new ShowPartitionsRequest { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        ShowPartitionsResponse response =
            await InvokeAsync(_grpcClient.ShowPartitionsAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        List<MilvusPartition> partitions = new();
        if (response.PartitionIDs is not null)
        {
            for (int i = 0; i < response.PartitionIDs.Count; i++)
            {
                partitions.Add(new MilvusPartition(
                    response.PartitionIDs[i],
                    response.PartitionNames[i],
                    TimestampUtils.GetTimeFromTimestamp((long)response.CreatedUtcTimestamps[i]),
                    response.InMemoryPercentages?.Count > 0 ? response.InMemoryPercentages[i] : -1));
            }
        }

        return partitions;
    }

    /// <summary>
    /// Load a group of partitions for search.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionNames">The partition names you want to load.</param>
    /// <param name="replicaNumber">The replicas number you would load, 1 by default.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task LoadPartitionsAsync(
        string collectionName,
        IList<string> partitionNames,
        int replicaNumber = 1,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);

        LoadPartitionsRequest request = new()
        {
            CollectionName = collectionName,
            ReplicaNumber = replicaNumber
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        request.PartitionNames.AddRange(partitionNames);

        await InvokeAsync(_grpcClient.LoadPartitionsAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Release a group of loaded partitions.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionNames">The partition names you want to release.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task ReleasePartitionAsync(
        string collectionName,
        IList<string> partitionNames,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);

        ReleasePartitionsRequest request = new() { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        request.PartitionNames.AddRange(partitionNames);

        await InvokeAsync(_grpcClient.ReleasePartitionsAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete a partition.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to drop.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DropPartitionsAsync(
        string collectionName,
        string partitionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);

        var request = new DropPartitionRequest { CollectionName = collectionName, PartitionName = partitionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.DropPartitionAsync, request, cancellationToken).ConfigureAwait(false);
    }
}
