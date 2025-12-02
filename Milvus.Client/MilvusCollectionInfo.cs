namespace Milvus.Client;

/// <summary>
/// Milvus collection information
/// </summary>
public sealed class MilvusCollectionInfo
{
    internal MilvusCollectionInfo(
        long id,
        string name,
        ulong creationTimestamp,
        long inMemoryPercentage)
    {
        Id = id;
        Name = name;
        CreationTimestamp = creationTimestamp;
        InMemoryPercentage = inMemoryPercentage;
    }

    /// <summary>
    /// Collection Id list.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Collection name list.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// An opaque identifier for the point in time in which the the collection was created. Can be passed to
    /// <see cref="MilvusCollection.SearchAsync{T}(string, IReadOnlyList{ReadOnlyMemory{T}}, SimilarityMetricType, int, SearchParameters, CancellationToken)" />
    /// or <see cref="MilvusCollection.QueryAsync" /> as a <i>guarantee timestamp</i> or as a <i>time travel timestamp</i>.
    /// </summary>
    public ulong CreationTimestamp { get; }

    /// <summary>
    /// Load percentage on query node when type is InMemory.
    /// </summary>
    public long InMemoryPercentage { get; }

    /// <summary>
    /// Return string value of <see cref="MilvusCollectionInfo"/>.
    /// </summary>
    public override string ToString()
        => $"MilvusCollection: {{{nameof(Name)}: {Name}, {nameof(Id)}: {Id}, {nameof(CreationTimestamp)}:{CreationTimestamp}, {nameof(InMemoryPercentage)}: {InMemoryPercentage}}}";
}
