using IO.Milvus.Grpc;
using IO.Milvus.Utils;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates a partition.
    /// </summary>
    /// <param name="collectionName">The name of the collection in which the partition is to be created.</param>
    /// <param name="partitionName">The name of partition to be created.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Checks whether a partition exists.
    /// </summary>
    /// <param name="collectionName">The name of the partition's collection.</param>
    /// <param name="partitionName">The name of the partition to be checked.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Lists all partitions defined for a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection whose partitions are to be listed.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Loads a partition into memory so that it can be searched or queries.
    /// </summary>
    /// <param name="collectionName">The name of the collection whose partition is to be loaded.</param>
    /// <param name="partitionName">The name of the partition to be loaded.</param>
    /// <param name="replicaNumber">An optional replica number to load.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public Task LoadPartitionsAsync(
        string collectionName,
        string partitionName,
        int replicaNumber = 1,
        string? dbName = null,
        CancellationToken cancellationToken = default)
        => LoadPartitionsAsync(collectionName, new[] { partitionName }, replicaNumber, dbName, cancellationToken);

    /// <summary>
    /// Loads partitions into memory so that they can be searched or queries.
    /// </summary>
    /// <param name="collectionName">The name of the collection whose partitions are to be loaded.</param>
    /// <param name="partitionNames">The names of the partition to be loaded.</param>
    /// <param name="replicaNumber">An optional replica number to load.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Releases a loaded partition from memory.
    /// </summary>
    /// <param name="collectionName">The name of the collection whose partition is to be released.</param>
    /// <param name="partitionName">The name of the partition to be released.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public Task ReleasePartitionAsync(
        string collectionName,
        string partitionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
        => ReleasePartitionAsync(collectionName, new[] { partitionName }, dbName, cancellationToken);

    /// <summary>
    /// Releases loaded partitions from memory.
    /// </summary>
    /// <param name="collectionName">The name of the collection whose partitions are to be released.</param>
    /// <param name="partitionNames">The names of the partitions to be released.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Drops a partition.
    /// </summary>
    /// <param name="collectionName">The name of the collection whose partition is to be dropped.</param>
    /// <param name="partitionName">The name of the partition to be dropped.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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

    /// <summary>
    /// Retrieves statistics for a partition.
    /// </summary>
    /// <param name="collectionName">The name of the collection for the partition.</param>
    /// <param name="partitionName">The name of partition for which statistics are to be retrieved.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<IDictionary<string, string>> GetPartitionStatisticsAsync(
        string collectionName,
        string partitionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(partitionName);

        var request = new GetPartitionStatisticsRequest
        {
            CollectionName = collectionName,
            PartitionName = partitionName
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        GetPartitionStatisticsResponse response =
            await InvokeAsync(_grpcClient.GetPartitionStatisticsAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Stats.ToDictionary(static p => p.Key, static p => p.Value);
    }
}
