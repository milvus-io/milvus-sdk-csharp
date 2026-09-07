namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a <c>ListCollections</c> operation.
/// </summary>
public sealed class ListCollectionsResp
{
    private ListCollectionsResp(IReadOnlyList<string> collectionNames, IReadOnlyList<long> collectionIds)
    {
        CollectionNames = collectionNames;
        CollectionIds = collectionIds;
    }

    internal static ListCollectionsResp FromGrpc(Grpc.ShowCollectionsResponse response)
        => new(response.CollectionNames.ToList(), response.CollectionIds.ToList());

    /// <summary>
    /// The names of the collections.
    /// </summary>
    public IReadOnlyList<string> CollectionNames { get; }

    /// <summary>
    /// The ids of the collections.
    /// </summary>
    public IReadOnlyList<long> CollectionIds { get; }
}
