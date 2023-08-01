namespace Milvus.Client;

public partial class MilvusCollection
{
    /// <summary>
    /// Creates a partition.
    /// </summary>
    /// <param name="partitionName">The name of partition to be created.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task CreatePartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(partitionName);

        var request = new CreatePartitionRequest { CollectionName = Name, PartitionName = partitionName };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        await _client.InvokeAsync(_client.GrpcClient.CreatePartitionAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Checks whether a partition exists.
    /// </summary>
    /// <param name="partitionName">The name of the partition to be checked.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<bool> HasPartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(partitionName);

        var request = new HasPartitionRequest { CollectionName = Name, PartitionName = partitionName };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        BoolResponse response =
            await _client.InvokeAsync(_client.GrpcClient.HasPartitionAsync, request, static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        return response.Value;
    }

    /// <summary>
    /// Lists all partitions defined for a collection.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<IList<MilvusPartition>> ShowPartitionsAsync(CancellationToken cancellationToken = default)
    {
        var request = new ShowPartitionsRequest { CollectionName = Name };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        ShowPartitionsResponse response =
            await _client.InvokeAsync(_client.GrpcClient.ShowPartitionsAsync, request, static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        List<MilvusPartition> partitions = new();
        if (response.PartitionIDs is not null)
        {
            for (int i = 0; i < response.PartitionIDs.Count; i++)
            {
                partitions.Add(new MilvusPartition(
                    response.PartitionIDs[i],
                    response.PartitionNames[i],
                    response.CreatedUtcTimestamps[i],
                    response.InMemoryPercentages?.Count > 0 ? response.InMemoryPercentages[i] : -1));
            }
        }

        return partitions;
    }

    /// <summary>
    /// Loads a partition into memory so that it can be searched or queries.
    /// </summary>
    /// <param name="partitionName">The name of the partition to be loaded.</param>
    /// <param name="replicaNumber">An optional replica number to load.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public Task LoadPartitionsAsync(
        string partitionName,
        int replicaNumber = 1,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(partitionName);

        return LoadPartitionsAsync(new[] { partitionName }, replicaNumber, cancellationToken);
    }

    /// <summary>
    /// Loads partitions into memory so that they can be searched or queries.
    /// </summary>
    /// <param name="partitionNames">The names of the partition to be loaded.</param>
    /// <param name="replicaNumber">An optional replica number to load.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task LoadPartitionsAsync(
        IReadOnlyList<string> partitionNames,
        int replicaNumber = 1,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(partitionNames);

        LoadPartitionsRequest request = new()
        {
            CollectionName = Name,
            ReplicaNumber = replicaNumber
        };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        request.PartitionNames.AddRange(partitionNames);

        await _client.InvokeAsync(_client.GrpcClient.LoadPartitionsAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Releases a loaded partition from memory.
    /// </summary>
    /// <param name="partitionName">The name of the partition to be released.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public Task ReleasePartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(partitionName);

        return ReleasePartitionAsync(new[] { partitionName }, cancellationToken);
    }

    /// <summary>
    /// Releases loaded partitions from memory.
    /// </summary>
    /// <param name="partitionNames">The names of the partitions to be released.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task ReleasePartitionAsync(
        IReadOnlyList<string> partitionNames,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(partitionNames);

        ReleasePartitionsRequest request = new() { CollectionName = Name };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        request.PartitionNames.AddRange(partitionNames);

        await _client.InvokeAsync(_client.GrpcClient.ReleasePartitionsAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a partition.
    /// </summary>
    /// <param name="partitionName">The name of the partition to be dropped.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task DropPartitionsAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(partitionName);

        var request = new DropPartitionRequest { CollectionName = Name, PartitionName = partitionName };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        await _client.InvokeAsync(_client.GrpcClient.DropPartitionAsync, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves statistics for a partition.
    /// </summary>
    /// <param name="partitionName">The name of partition for which statistics are to be retrieved.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns></returns>
    public async Task<IDictionary<string, string>> GetPartitionStatisticsAsync(
        string partitionName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(partitionName);

        var request = new GetPartitionStatisticsRequest
        {
            CollectionName = Name,
            PartitionName = partitionName
        };

        if (DatabaseName is not null)
        {
            request.DbName = DatabaseName;
        }

        GetPartitionStatisticsResponse response =
            await _client.InvokeAsync(_client.GrpcClient.GetPartitionStatisticsAsync, request, static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        return response.Stats.ToDictionary(static p => p.Key, static p => p.Value);
    }
}
