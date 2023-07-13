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
        string dbName = Constants.DefaultDatabaseName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.CreatePartitionAsync, new CreatePartitionRequest
        {
            CollectionName = collectionName,
            PartitionName = partitionName,
            DbName = dbName,
        }, cancellationToken).ConfigureAwait(false);
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
        string dbName = Constants.DefaultDatabaseName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        BoolResponse response = await InvokeAsync(_grpcClient.HasPartitionAsync, new HasPartitionRequest
        {
            CollectionName = collectionName,
            PartitionName = partitionName,
            DbName = dbName,
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

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
        string dbName = Constants.DefaultDatabaseName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(dbName);

        ShowPartitionsResponse response = await InvokeAsync(_grpcClient.ShowPartitionsAsync, new ShowPartitionsRequest
        {
            CollectionName = collectionName,
            DbName = dbName
        }, static r => r.Status, cancellationToken).ConfigureAwait(false);

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
        string dbName = Constants.DefaultDatabaseName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        LoadPartitionsRequest request = new()
        {
            CollectionName = collectionName,
            ReplicaNumber = replicaNumber,
            DbName = dbName
        };
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
        string dbName = Constants.DefaultDatabaseName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrEmpty(partitionNames);
        Verify.NotNullOrWhiteSpace(dbName);

        ReleasePartitionsRequest request = new()
        {
            CollectionName = collectionName,
            DbName = dbName
        };
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
        string dbName = Constants.DefaultDatabaseName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropPartitionAsync, new DropPartitionRequest
        {
            CollectionName = collectionName,
            PartitionName = partitionName
        }, cancellationToken).ConfigureAwait(false);
    }
}
