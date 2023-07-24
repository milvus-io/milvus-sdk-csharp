using IO.Milvus.Client;

namespace IO.Milvus;

/// <summary>
/// Milvus collection information
/// </summary>
public sealed class MilvusCollection
{
    internal MilvusCollection(
        long id,
        string name,
        ulong creationTimestamp,
        long inMemoryPercentage)
    {
        CollectionId = id;
        CollectionName = name;
        CreationTimestamp = creationTimestamp;
        InMemoryPercentage = inMemoryPercentage;
    }

    /// <summary>
    /// Collection Id list.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// Collection name list.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// An opaque identifier for the point in time in which the the collection was created. Can be passed to
    /// <see cref="MilvusClient.SearchAsync{T}" /> or <see cref="MilvusClient.QueryAsync" /> as a <i>guarantee
    /// timestamp</i> or as a <i>time travel timestamp</i>.
    /// </summary>
    public ulong CreationTimestamp { get; }

    /// <summary>
    /// Load percentage on query node when type is InMemory.
    /// </summary>
    public long InMemoryPercentage { get; }

    /// <summary>
    /// Return string value of <see cref="MilvusCollection"/>.
    /// </summary>
    public override string ToString()
        => $"MilvusCollection: {{{nameof(CollectionName)}: {CollectionName}, {nameof(CollectionId)}: {CollectionId}, {nameof(CreationTimestamp)}:{CreationTimestamp}, {nameof(InMemoryPercentage)}: {InMemoryPercentage}}}";
}
