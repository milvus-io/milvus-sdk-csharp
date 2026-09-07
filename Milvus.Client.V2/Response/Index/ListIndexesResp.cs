namespace Milvus.Client.V2.Responses.Index;

/// <summary>
/// Represents the result of a <c>ListIndexes</c> operation.
/// </summary>
public sealed class ListIndexesResp
{
    internal ListIndexesResp(IReadOnlyList<IndexDescription> indexes)
    {
        Indexes = indexes;
    }

    internal static ListIndexesResp FromGrpc(Grpc.DescribeIndexResponse response)
        => new(response.IndexDescriptions.Select(IndexDescription.FromGrpc).ToList());

    /// <summary>
    /// The descriptions of the indexes on the collection.
    /// </summary>
    public IReadOnlyList<IndexDescription> Indexes { get; }
}
