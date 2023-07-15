namespace IO.Milvus;

/// <summary>
/// Describe a milvus collection
/// </summary>
public sealed class DetailedMilvusCollection
{
    internal DetailedMilvusCollection(
        IReadOnlyList<string> aliases,
        string collectionName,
        long collectionId,
        MilvusConsistencyLevel consistencyLevel,
        DateTime createdUtcTime,
        CollectionSchema schema,
        int shardsNum,
        Dictionary<string, IList<int>> startPositions)
    {
        Aliases = aliases;
        CollectionName = collectionName;
        CollectionId = collectionId;
        ConsistencyLevel = consistencyLevel;
        CreatedUtcTime = createdUtcTime;
        Schema = schema;
        ShardsNum = shardsNum;
        StartPositions = startPositions;
    }

    /// <summary>
    /// The aliases of this collection.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// The collection name.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The collection id.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// Consistency level.
    /// </summary>
    /// <remarks>
    /// The consistency level that the collection used, modification is not supported now.
    /// </remarks>
    public MilvusConsistencyLevel ConsistencyLevel { get; }

    /// <summary>
    /// The utc timestamp calculated by created_timestamp.
    /// </summary>
    public DateTime CreatedUtcTime { get; }

    /// <summary>
    /// Collection Schema.
    /// </summary>
    public CollectionSchema Schema { get; }

    /// <summary>
    /// The shards number you set.
    /// </summary>
    public int ShardsNum { get; }

    /// <summary>
    /// The message ID/position when collection is created.
    /// </summary>
    public IDictionary<string, IList<int>> StartPositions { get; }
}