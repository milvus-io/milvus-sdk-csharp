namespace Milvus.Client;

/// <summary>
/// Describe a milvus collection
/// </summary>
public sealed class MilvusCollectionDescription
{
    internal MilvusCollectionDescription(
        IReadOnlyList<string> aliases,
        string collectionName,
        long collectionId,
        ConsistencyLevel consistencyLevel,
        ulong creationTimestamp,
        CollectionSchema schema,
        int shardsNum,
        IReadOnlyDictionary<string, IList<int>> startPositions)
    {
        Aliases = aliases;
        CollectionName = collectionName;
        CollectionId = collectionId;
        ConsistencyLevel = consistencyLevel;
        CreationTimestamp = creationTimestamp;
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
    public ConsistencyLevel ConsistencyLevel { get; }

    /// <summary>
    /// An opaque identifier for the point in time in which the the collection was created. Can be passed to
    /// <see cref="MilvusCollection.SearchAsync{T}(string, IReadOnlyList{ReadOnlyMemory{T}}, SimilarityMetricType, int, SearchParameters, CancellationToken)" />
    /// or <see cref="MilvusCollection.QueryAsync" /> as a <i>guarantee timestamp</i> or as a <i>time travel timestamp</i>.
    /// </summary>
    public ulong CreationTimestamp { get; }

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
    public IReadOnlyDictionary<string, IList<int>> StartPositions { get; }
}
