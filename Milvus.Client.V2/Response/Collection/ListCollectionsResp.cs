namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a <c>ListCollections</c> operation.
/// </summary>
public sealed class ListCollectionsResp
{
    private ListCollectionsResp(
        IReadOnlyList<string> collectionNames, IReadOnlyList<long> collectionIds,
        IReadOnlyList<ulong> createdTimestamps, IReadOnlyList<ulong> createdUtcTimestamps,
        IReadOnlyList<long> inMemoryPercentages)
    {
        CollectionNames = collectionNames;
        CollectionIds = collectionIds;
        CreatedTimestamps = createdTimestamps;
        CreatedUtcTimestamps = createdUtcTimestamps;
        InMemoryPercentages = inMemoryPercentages;
    }

    internal static ListCollectionsResp FromGrpc(Grpc.ShowCollectionsResponse response)
        => new(
            response.CollectionNames.ToList(),
            response.CollectionIds.ToList(),
            response.CreatedTimestamps.ToList(),
            response.CreatedUtcTimestamps.ToList(),
#pragma warning disable CS0612 // InMemoryPercentages is [deprecated] in the proto but read for C++ parity.
            response.InMemoryPercentages.ToList());
#pragma warning restore CS0612

    /// <summary>
    /// The names of the collections.
    /// </summary>
    public IReadOnlyList<string> CollectionNames { get; }

    /// <summary>
    /// The ids of the collections.
    /// </summary>
    public IReadOnlyList<long> CollectionIds { get; }

    /// <summary>
    /// The hybrid timestamps at which the collections were created, aligned with <see cref="CollectionNames" />.
    /// </summary>
    public IReadOnlyList<ulong> CreatedTimestamps { get; }

    /// <summary>
    /// The UTC timestamps at which the collections were created, aligned with <see cref="CollectionNames" />.
    /// </summary>
    public IReadOnlyList<ulong> CreatedUtcTimestamps { get; }

    /// <summary>
    /// The percentage of each collection loaded into memory, aligned with <see cref="CollectionNames" />.
    /// </summary>
    public IReadOnlyList<long> InMemoryPercentages { get; }
}
