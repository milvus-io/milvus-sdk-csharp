using System.Globalization;
using IO.Milvus.Grpc;

namespace IO.Milvus;

public partial class MilvusDatabase
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
    /// The consistency level to be used by the collection. Defaults to <see cref="ConsistencyLevel.Session" />.
    /// </param>
    /// <param name="shardsNum">Number of the shards for the collection to create.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public Task<MilvusCollection> CreateCollectionAsync(
        string collectionName,
        IList<FieldSchema> fields,
        ConsistencyLevel consistencyLevel = ConsistencyLevel.Session,
        int shardsNum = 1,
        CancellationToken cancellationToken = default)
    {
        CollectionSchema schema = new();
        schema.Fields.AddRange(fields);
        return CreateCollectionAsync(collectionName, schema, consistencyLevel, shardsNum, cancellationToken);
    }

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to create.</param>
    /// <param name="schema">The schema definition for the collection.</param>
    /// <param name="consistencyLevel">
    /// The consistency level to be used by the collection. Defaults to <see cref="ConsistencyLevel.Session" />.
    /// </param>
    /// <param name="shardsNum">Number of the shards for the collection to create.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<MilvusCollection> CreateCollectionAsync(
        string collectionName,
        CollectionSchema schema,
        ConsistencyLevel consistencyLevel = ConsistencyLevel.Session,
        int shardsNum = 1,
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
            ConsistencyLevel = (Grpc.ConsistencyLevel)(int)consistencyLevel,
            ShardsNum = shardsNum,
            Schema = grpcCollectionSchema.ToByteString()
        };

        if (Name is not null)
        {
            request.DbName = Name;
        }

        await _client.InvokeAsync(_client.GrpcClient.CreateCollectionAsync, request, cancellationToken)
            .ConfigureAwait(false);

        return new MilvusCollection(_client, collectionName, Name);
    }

    /// <summary>
    /// Checks whether a collection exists in the database.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="timestamp">
    /// If non-zero, returns <c>true</c> only if the collection was created before the given timestamp.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<bool> HasCollectionAsync(
        string collectionName,
        ulong timestamp = 0,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        var request = new HasCollectionRequest
        {
            CollectionName = collectionName,
            TimeStamp = timestamp,
        };

        if (Name is not null)
        {
            request.DbName = Name;
        }

        BoolResponse response =
            await _client.InvokeAsync(_client.GrpcClient.HasCollectionAsync, request, static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        return response.Value;
    }

    /// <summary>
    /// Lists the collections available in the database.
    /// </summary>
    /// <param name="collectionNames">An optional list of collection names by which to filter.</param>
    /// <param name="showType">
    /// Determines whether all collections are returned, or only ones which have been loaded to memory.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<IList<MilvusCollectionInfo>> ShowCollectionsAsync(
        IEnumerable<string>? collectionNames = null,
        ShowType showType = ShowType.All,
        CancellationToken cancellationToken = default)
    {
        ShowCollectionsRequest request = new() { Type = (Grpc.ShowType)showType, };

        if (Name is not null)
        {
            request.DbName = Name;
        }

        if (collectionNames is not null)
        {
            request.CollectionNames.AddRange(collectionNames);
        }

        ShowCollectionsResponse response =
            await _client.InvokeAsync(_client.GrpcClient.ShowCollectionsAsync, request, static r => r.Status,
                    cancellationToken)
                .ConfigureAwait(false);

        List<MilvusCollectionInfo> collections = new();
        if (response.CollectionIds is not null)
        {
            for (int i = 0; i < response.CollectionIds.Count; i++)
            {
                collections.Add(new MilvusCollectionInfo(
                    response.CollectionIds[i],
                    response.CollectionNames[i],
                    response.CreatedUtcTimestamps[i],
                    response.InMemoryPercentages?.Count > 0 ? response.InMemoryPercentages[i] : -1));
            }
        }

        return collections;
    }
}
