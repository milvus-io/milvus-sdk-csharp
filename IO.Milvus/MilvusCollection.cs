namespace IO.Milvus;

/// <summary>
/// Milvus collection information
/// </summary>
public sealed class MilvusCollection
{
    internal MilvusCollection(
        long id,
        string name,
        DateTime createdUtcTime,
        long inMemoryPercentage)
    {
        CollectionId = id;
        CollectionName = name;
        CreatedUtcTime = createdUtcTime;
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
    /// The utc timestamp calculated by created_timestamp.
    /// </summary>
    public DateTime CreatedUtcTime { get; }

    /// <summary>
    /// Load percentage on query node when type is InMemory.
    /// </summary>
    public long InMemoryPercentage { get; }

    /// <summary>
    /// Return string value of <see cref="MilvusCollection"/>.
    /// </summary>
    public override string ToString()
        => $"MilvusCollection: {{{nameof(CollectionName)}: {CollectionName}, {nameof(CollectionId)}: {CollectionId}, {nameof(CreatedUtcTime)}:{CreatedUtcTime}, {nameof(InMemoryPercentage)}: {InMemoryPercentage}}}";
}