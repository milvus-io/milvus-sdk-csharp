namespace Milvus.Client.V2.Responses.Index;

/// <summary>
/// Represents the result of a <c>ListIndexes</c> operation.
/// </summary>
public sealed class ListIndexesResp
{
    internal ListIndexesResp(IReadOnlyList<IndexDesc> indexes)
    {
        Indexes = indexes;
        IndexNames = indexes.Select(i => i.IndexName).ToList();
    }

    internal static ListIndexesResp FromGrpc(Grpc.DescribeIndexResponse response)
        => new(response.IndexDescriptions.Select(IndexDesc.FromGrpc).ToList());

    // Returns a copy with a filtered index list (used by the client-side FieldName filter).
    internal static ListIndexesResp WithIndexes(IReadOnlyList<IndexDesc> indexes)
        => new(indexes);

    /// <summary>
    /// The descriptions of the indexes on the collection.
    /// </summary>
    public IReadOnlyList<IndexDesc> Indexes { get; }

    /// <summary>
    /// The names of the indexes on the collection, in the same order as <see cref="Indexes" />. Mirrors the C++
    /// SDK's <c>ListIndexesResponse::IndexNames</c>.
    /// </summary>
    public IReadOnlyList<string> IndexNames { get; }
}
