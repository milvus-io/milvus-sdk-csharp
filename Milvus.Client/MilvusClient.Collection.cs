using System.Globalization;

namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Returns a <see cref="MilvusCollection" /> representing a Milvus collection in the default database. This is the
    /// starting point for all on which all collection operations.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    public MilvusCollection GetCollection(string collectionName)
    {
        Verify.NotNullOrWhiteSpace(collectionName);

        return new MilvusCollection(this, collectionName);
    }

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
        IReadOnlyList<FieldSchema> fields,
        ConsistencyLevel consistencyLevel = ConsistencyLevel.Session,
        int shardsNum = 1,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNull(fields);

        CollectionSchema schema = new(fields);
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
            EnableDynamicField = schema.EnableDynamicFields

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
                ElementType = (DataType)(int)field.ElementDataType,
                IsPrimaryKey = field.IsPrimaryKey,
                IsPartitionKey = field.IsPartitionKey,
                AutoID = field.AutoId,
                Nullable = field.Nullable,
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

            if (field.MaxCapacity is not null)
            {
                grpcField.TypeParams.Add(new Grpc.KeyValuePair
                {
                    Key = Constants.MaxCapacity,
                    Value = field.MaxCapacity.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (field.DefaultValue is not null)
            {
                grpcField.DefaultValue = ConvertToValueField(field.DefaultValue, field.DataType);
            }

            grpcCollectionSchema.Fields.Add(grpcField);
        }

#pragma warning disable CS0612 // Schema-level AutoID is obsolete, but still there in pymilvus
        grpcCollectionSchema.AutoID = schema.Fields.Any(static p => p.AutoId);
#pragma warning restore CS0612 // Type or member is obsolete

        var request = new CreateCollectionRequest
        {
            CollectionName = collectionName,
            ConsistencyLevel = (Grpc.ConsistencyLevel)(int)consistencyLevel,
            ShardsNum = shardsNum,
            Schema = grpcCollectionSchema.ToByteString()
        };

        await InvokeAsync(GrpcClient.CreateCollectionAsync, request, cancellationToken).ConfigureAwait(false);

        return new MilvusCollection(this, collectionName);
     }

    /// <summary>
    /// Checks whether a collection exists.
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

        BoolResponse response =
            await InvokeAsync(GrpcClient.HasCollectionAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return response.Value;
    }

    /// <summary>
    /// Lists the collections available in the database.
    /// </summary>
    /// <param name="collectionNames">An optional list of collection names by which to filter.</param>
    /// <param name="filter">
    /// Determines whether all collections are returned, or only ones which have been loaded to memory.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    // ShowType and CollectionNames are obsolete, but seem required and is still used in pymilvus
#pragma warning disable CS0612
    public async Task<IReadOnlyList<MilvusCollectionInfo>> ListCollectionsAsync(
        IReadOnlyList<string>? collectionNames = null,
        CollectionFilter filter = CollectionFilter.All,
        CancellationToken cancellationToken = default)
    {
        ShowCollectionsRequest request = new() { Type = (Grpc.ShowType)filter };

        if (collectionNames is not null)
        {
            request.CollectionNames.AddRange(collectionNames);
        }

        ShowCollectionsResponse response =
            await InvokeAsync(GrpcClient.ShowCollectionsAsync, request, static r => r.Status, cancellationToken)
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
#pragma warning restore CS0612

    /// <summary>
    /// Flushes collection data to disk, required only in order to get up-to-date statistics.
    /// </summary>
    /// <remarks>
    /// This method will be removed in a future version.
    /// </remarks>
    /// <param name="collectionNames">The names of the collections to be flushed.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    public async Task<FlushResult> FlushAsync(
        IReadOnlyList<string> collectionNames,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(collectionNames);

        FlushRequest request = new();

        request.CollectionNames.AddRange(collectionNames);

        FlushResponse response =
            await InvokeAsync(GrpcClient.FlushAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return FlushResult.From(response);
    }

    private static Grpc.ValueField ConvertToValueField(object value, MilvusDataType dataType)
    {
        Grpc.ValueField valueField = new();

        switch (dataType)
        {
            case MilvusDataType.Bool:
                valueField.BoolData = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                break;
            case MilvusDataType.Int8:
            case MilvusDataType.Int16:
            case MilvusDataType.Int32:
                valueField.IntData = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                break;
            case MilvusDataType.Int64:
                valueField.LongData = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                break;
            case MilvusDataType.Float:
                valueField.FloatData = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                break;
            case MilvusDataType.Double:
                valueField.DoubleData = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                break;
            case MilvusDataType.String:
            case MilvusDataType.VarChar:
                valueField.StringData = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                break;
            default:
                throw new ArgumentException(
                    $"Default values are not supported for {dataType} fields. Only scalar fields support default values.",
                    nameof(value));
        }

        return valueField;
    }
}
