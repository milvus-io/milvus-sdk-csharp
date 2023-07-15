using IO.Milvus.Grpc;
using IO.Milvus.Utils;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Create a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="consistencyLevel">
    /// The consistency level that the collection used, modification is not supported now.</param>
    /// <param name="fieldTypes">field types that represents this collection schema</param>
    /// <param name="shardsNum">Once set, no modification is allowed (Optional).</param>
    /// <param name="enableDynamicField"><see href="https://milvus.io/docs/dynamic_schema.md#JSON-a-new-data-type"/></param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CreateCollectionAsync(
        string collectionName,
        IList<FieldType> fieldTypes,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session,
        int shardsNum = 1,
        bool enableDynamicField = false,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        // Validate field types
        FieldType firstField = fieldTypes[0];
        if (!firstField.IsPrimaryKey ||
            firstField.DataType is not (MilvusDataType)DataType.Int64 and not (MilvusDataType)DataType.VarChar)
        {
            throw new MilvusException("The first FieldType's IsPrimaryKey must be true and DataType == Int64 or DataType == VarChar");
        }

        for (int i = 1; i < fieldTypes.Count; i++)
        {
            if (fieldTypes[i].IsPrimaryKey)
            {
                throw new ArgumentException("FieldTypes needs at most one primary key field type", nameof(fieldTypes));
            }
        }

        var schema = new CollectionSchema
        {
            Name = collectionName,
            Fields = fieldTypes,
            EnableDynamicField = enableDynamicField
        };

        var request = new CreateCollectionRequest
        {
            CollectionName = collectionName,
            ConsistencyLevel = (ConsistencyLevel)(int)consistencyLevel,
            ShardsNum = shardsNum,
            Schema = schema.ConvertCollectionSchema().ToByteString()
        };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.CreateCollectionAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Describe a collection.
    /// </summary>
    /// <param name="collectionName">collectionName</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<DetailedMilvusCollection> DescribeCollectionAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new DescribeCollectionRequest { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        DescribeCollectionResponse response =
            await InvokeAsync(_grpcClient.DescribeCollectionAsync, request, r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return new DetailedMilvusCollection(
            response.Aliases,
            response.CollectionName,
            response.CollectionID,
            (MilvusConsistencyLevel)response.ConsistencyLevel,
            TimestampUtils.GetTimeFromTimestamp((long)response.CreatedUtcTimestamp),
            response.Schema.ToCollectionSchema(),
            response.ShardsNum,
            response.StartPositions.ToKeyDataPairs());
    }

    /// <summary>
    /// Rename a collection.
    /// </summary>
    /// <param name="oldName">The old collection name.</param>
    /// <param name="newName">The new collection name.</param>
    /// <param name="dbName">Database name, available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RenameCollectionAsync(
        string oldName,
        string newName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(oldName);
        Verify.NotNullOrWhiteSpace(newName);

        var request = new RenameCollectionRequest { OldName = oldName, NewName = newName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.RenameCollectionAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drop a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.(Required).</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DropCollectionAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new DropCollectionRequest { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.DropCollectionAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get a collection's statistics
    /// </summary>
    /// <param name="collectionName">The collection name you want get statistics</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IDictionary<string, string>> GetCollectionStatisticsAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new GetCollectionStatisticsRequest { CollectionName = collectionName, };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        GetCollectionStatisticsResponse response = await InvokeAsync(
            _grpcClient.GetCollectionStatisticsAsync,
            request,
            static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.Stats.ToDictionary();
    }

    /// <summary>
    /// Get if a collection's existence
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="dateTime">
    /// If time_stamp is not zero,
    /// will return true when time_stamp >= created collection timestamp,
    /// otherwise will return false.
    /// </param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<bool> HasCollectionAsync(
        string collectionName,
        DateTime? dateTime = null,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new HasCollectionRequest
        {
            CollectionName = collectionName,
            TimeStamp = (ulong)(dateTime?.ToUtcTimestamp() ?? 0),
        };

        if (dbName is not null)
        {
            request.DbName = null;
        }

        BoolResponse response =
            await InvokeAsync(_grpcClient.HasCollectionAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Value;
    }

    /// <summary>
    /// The collection name you want to load.
    /// </summary>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="replicaNumber">The replica number to load, default by 1.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadCollectionAsync(
        string collectionName,
        int replicaNumber = 1,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.GreaterThanOrEqualTo(replicaNumber, 1);

        var request = new LoadCollectionRequest { CollectionName = collectionName, ReplicaNumber = replicaNumber };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.LoadCollectionAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Release a collection loaded before
    /// </summary>
    /// <param name="collectionName">The collection name you want to release.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ReleaseCollectionAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new ReleaseCollectionRequest { CollectionName = collectionName, DbName = dbName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.ReleaseCollectionAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Show all collections.
    /// </summary>
    /// <param name="collectionNames">
    /// When type is InMemory, will return these collection's inMemory_percentages.(Optional)
    /// </param>
    /// <param name="showType">Decide return Loaded collections or All collections(Optional)</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IList<MilvusCollection>> ShowCollectionsAsync(
        IList<string>? collectionNames = null,
        ShowType showType = ShowType.All,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        ShowCollectionsRequest request = new() { Type = (Grpc.ShowType)showType, };

        if (dbName is not null)
        {
            request.DbName = dbName;

        }
        if (collectionNames is not null)
        {
            request.CollectionNames.AddRange(collectionNames);
        }

        ShowCollectionsResponse response =
            await InvokeAsync(_grpcClient.ShowCollectionsAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        List<MilvusCollection> collections = new();
        if (response.CollectionIds is not null)
        {
            for (int i = 0; i < response.CollectionIds.Count; i++)
            {
                collections.Add(new MilvusCollection(
                    response.CollectionIds[i],
                    response.CollectionNames[i],
                    TimestampUtils.GetTimeFromTimestamp((long)response.CreatedUtcTimestamps[i]),
                    response.InMemoryPercentages?.Count > 0 ? response.InMemoryPercentages[i] : -1));
            }
        }

        return collections;
    }

    /// <summary>
    /// Get loading progress of a collection or it's partition.
    /// </summary>
    /// <remarks>
    /// Not support in restful api.
    /// </remarks>
    /// <param name="collectionName">Collection name of milvus.</param>
    /// <param name="partitionNames">Partition names.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task<long> GetLoadingProgressAsync(
       string collectionName,
       IList<string> partitionNames,
       string? dbName = null,
       CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        GetLoadingProgressRequest request = new() { CollectionName = collectionName, };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        if (partitionNames.Count > 0)
        {
            request.PartitionNames.AddRange(partitionNames);
        }

        GetLoadingProgressResponse response =
            await InvokeAsync(_grpcClient.GetLoadingProgressAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Progress;
    }

    /// <summary>
    /// Get a partition's statistics.
    /// </summary>
    /// <param name="collectionName">The collection name in milvus.</param>
    /// <param name="partitionName">The partition name you want to collect statistics.</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

        return response.Stats.ToDictionary();
    }
}
