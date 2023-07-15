using System.Globalization;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Create a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="fields">field types that represents this collection schema</param>
    /// <param name="consistencyLevel">
    /// The consistency level that the collection used, modification is not supported now.
    /// </param>
    /// <param name="shardsNum">Once set, no modification is allowed (Optional).</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task CreateCollectionAsync(
        string collectionName,
        IList<FieldSchema> fields,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session,
        int shardsNum = 1,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        CollectionSchema schema = new();
        schema.Fields.AddRange(fields);
        return CreateCollectionAsync(collectionName, schema, consistencyLevel, shardsNum, dbName, cancellationToken);
    }

    /// <summary>
    /// Create a collection.
    /// </summary>
    /// <param name="collectionName">The unique collection name in milvus.</param>
    /// <param name="schema">The schema definition for the collection.</param>
    /// <param name="consistencyLevel">
    /// The consistency level that the collection used, modification is not supported now.
    /// </param>
    /// <param name="shardsNum">Once set, no modification is allowed (Optional).</param>
    /// <param name="dbName">Database name,available in <c>Milvus 2.2.9</c></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CreateCollectionAsync(
        string collectionName,
        CollectionSchema schema,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session,
        int shardsNum = 1,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        Grpc.CollectionSchema grpcCollectionSchema = new()
        {
            Name = schema.Name ?? collectionName,
            EnableDynamicField = schema.EnableDynamicField

            // Note that an AutoId previously existed at the schema level, but is not deprecated.
            // AutoId is now only defined at the field level.
        };

        if (schema.Description is not null)
        {
            grpcCollectionSchema.Description = schema.Description;
        }

        foreach (FieldSchema field in schema.Fields)
        {
            Grpc.FieldSchema grpcField = new()
            {
                Name = field.Name,
                DataType = (DataType)(int)field.DataType,
                FieldID = field.FieldId,
                IsPrimaryKey = field.IsPrimaryKey,
                IsPartitionKey = field.IsPartitionKey,
                AutoID = field.AutoId,
                Description = field.Description
            };

            if (field.MaxLength is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.VarcharMaxLength,
                    Value = field.MaxLength.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (field.Dimension is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.VectorDim,
                    Value = field.Dimension.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            // TODO: IndexParams

            grpcCollectionSchema.Fields.Add(grpcField);
        }

        grpcCollectionSchema.AutoID = schema.Fields.Any(static p => p.AutoId);

        var request = new CreateCollectionRequest
        {
            CollectionName = collectionName,
            ConsistencyLevel = (ConsistencyLevel)(int)consistencyLevel,
            ShardsNum = shardsNum,
            Schema = grpcCollectionSchema.ToByteString()
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
    public async Task<MilvusCollectionDescription> DescribeCollectionAsync(
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

        CollectionSchema milvusCollectionSchema = new()
        {
            Name = response.Schema.Name,
            Description = response.Schema.Description

            // Note that an AutoId previously existed at the schema level, but is not deprecated.
            // AutoId is now only defined at the field level.
        };

        foreach (Grpc.FieldSchema grpcField in response.Schema.Fields)
        {
            FieldSchema milvusField = new(
                grpcField.FieldID, grpcField.Name, (MilvusDataType)grpcField.DataType,
                (MilvusFieldState)grpcField.State, grpcField.IsPrimaryKey, grpcField.AutoID, grpcField.IsPartitionKey,
                grpcField.IsDynamic, grpcField.Description);

            foreach (Grpc.KeyValuePair parameter in grpcField.TypeParams)
            {
                switch (parameter.Key)
                {
                    case Constants.VarcharMaxLength:
                        milvusField.MaxLength = int.Parse(parameter.Value);
                        break;

                    case Constants.VectorDim:
                        milvusField.Dimension = long.Parse(parameter.Value);
                        break;

                    // TODO: Should we warn for unknown type params?
                }
            }

            // TODO: IndexParams

            milvusCollectionSchema.Fields.Add(milvusField);
        }

        Dictionary<string, IList<int>> startPositions = response.StartPositions.ToDictionary(
            kdp => kdp.Key,
            kdp => (IList<int>)kdp.Data.Select(static p => (int)p).ToList());

        return new MilvusCollectionDescription(
            response.Aliases,
            response.CollectionName,
            response.CollectionID,
            (MilvusConsistencyLevel)response.ConsistencyLevel,
            TimestampUtils.GetTimeFromTimestamp((long)response.CreatedUtcTimestamp),
            milvusCollectionSchema,
            response.ShardsNum,
            startPositions);
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
