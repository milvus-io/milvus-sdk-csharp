using System.Globalization;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="fields">
    /// Schema of the fields within the collection to create. Refer to
    /// <see href="https://milvus.io/docs/schema.md" /> for more information.
    /// </param>
    /// <param name="consistencyLevel">
    /// The consistency level to be used by the collection. Defaults to <see cref="MilvusConsistencyLevel.Session" />.
    /// </param>
    /// <param name="shardsNum">Number of the shards for the collection to create.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Creates a new collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="schema">The schema definition for the collection.</param>
    /// <param name="consistencyLevel">
    /// The consistency level to be used by the collection. Defaults to <see cref="MilvusConsistencyLevel.Session" />.
    /// </param>
    /// <param name="shardsNum">Number of the shards for the collection to create.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task CreateCollectionAsync(
        string collectionName,
        CollectionSchema schema,
        MilvusConsistencyLevel consistencyLevel = MilvusConsistencyLevel.Session,
        int shardsNum = 1,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNull(schema);

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
    /// Describes a collection, returning information about its configuration and schema.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
                        milvusField.MaxLength = int.Parse(parameter.Value, CultureInfo.InvariantCulture);
                        break;

                    case Constants.VectorDim:
                        milvusField.Dimension = long.Parse(parameter.Value, CultureInfo.InvariantCulture);
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
    /// Renames a collection.
    /// </summary>
    /// <param name="oldName">The old collection name.</param>
    /// <param name="newName">The new collection name.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Drops a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Retrieves statistics for a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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

        return response.Stats.ToDictionary(static p => p.Key, static p => p.Value);
    }

    /// <summary>
    /// Checks whether a collection exists.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="dateTime">
    /// If non-null, returns <c>true</c> only if the collection was created before the given timestamp.
    /// </param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
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
    /// Loads a collection into memory so that it can be searched or queried.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="replicaNumber">An optional replica number to load.</param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task LoadCollectionAsync(
        string collectionName,
        int? replicaNumber = null,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new LoadCollectionRequest { CollectionName = collectionName };

        if (replicaNumber is not null)
        {
            Verify.GreaterThanOrEqualTo(replicaNumber.Value, 1);

            request.ReplicaNumber = replicaNumber.Value;
        }

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
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ReleaseCollectionAsync(
        string collectionName,
        string? dbName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new ReleaseCollectionRequest { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        await InvokeAsync(_grpcClient.ReleaseCollectionAsync, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists the collections available in the database.
    /// </summary>
    /// <param name="collectionNames">An optional list of collection names by which to filter.</param>
    /// <param name="showType">
    /// Determines whether all collections are returned, or only ones which have been loaded to memory.
    /// </param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<IList<MilvusCollection>> ShowCollectionsAsync(
        IEnumerable<string>? collectionNames = null,
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
    /// Returns the loading progress for a collection, and optionally one or more of its partitions.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="partitionNames">
    /// An optional list of partition names for which to check the loading progress.
    /// </param>
    /// <param name="dbName">The database name. Available starting Milvus 2.2.9.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<long> GetLoadingProgressAsync(
       string collectionName,
       IEnumerable<string>? partitionNames = null,
       string? dbName = null,
       CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        GetLoadingProgressRequest request = new() { CollectionName = collectionName };

        if (dbName is not null)
        {
            request.DbName = dbName;
        }

        if (partitionNames is not null)
        {
            request.PartitionNames.AddRange(partitionNames);
        }

        GetLoadingProgressResponse response =
            await InvokeAsync(_grpcClient.GetLoadingProgressAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Progress;
    }
}
