namespace Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Returns a <see cref="MilvusCollection" /> representing a Milvus collection in the default database. This is the
    /// starting point for all on which all collection operations.
    /// </summary>
    /// <param name="collectionName">The name of the collection.</param>
    public MilvusCollection GetCollection(string collectionName)
        => new MilvusCollection(this, collectionName, databaseName: null);

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
        => _defaultDatabase.CreateCollectionAsync(
            collectionName, fields, consistencyLevel, shardsNum, cancellationToken);

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
    public Task<MilvusCollection> CreateCollectionAsync(
        string collectionName,
        CollectionSchema schema,
        ConsistencyLevel consistencyLevel = ConsistencyLevel.Session,
        int shardsNum = 1,
        CancellationToken cancellationToken = default)
        => _defaultDatabase.CreateCollectionAsync(
            collectionName, schema, consistencyLevel, shardsNum, cancellationToken);

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
    public Task<bool> HasCollectionAsync(
        string collectionName,
        ulong timestamp = 0,
        CancellationToken cancellationToken = default)
        => _defaultDatabase.HasCollectionAsync(collectionName, timestamp, cancellationToken);

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
    public Task<IReadOnlyList<MilvusCollectionInfo>> ListCollectionsAsync(
        IReadOnlyList<string>? collectionNames = null,
        ListCollectionFilter filter = ListCollectionFilter.All,
        CancellationToken cancellationToken = default)
        => _defaultDatabase.ShowCollectionsAsync(collectionNames, filter, cancellationToken);

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
    public Task<MilvusFlushResult> FlushAsync(
        IReadOnlyList<string> collectionNames,
        CancellationToken cancellationToken = default)
        => FlushAsync(collectionNames, databaseName: null, cancellationToken);

    internal async Task<MilvusFlushResult> FlushAsync(
        IReadOnlyList<string> collectionNames,
        string? databaseName,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrEmpty(collectionNames);

        FlushRequest request = new();

        if (databaseName is not null)
        {
            request.DbName = databaseName;
        }

        request.CollectionNames.AddRange(collectionNames);

        FlushResponse response =
            await InvokeAsync(GrpcClient.FlushAsync, request, static r => r.Status, cancellationToken)
                .ConfigureAwait(false);

        return MilvusFlushResult.From(response);
    }
}
