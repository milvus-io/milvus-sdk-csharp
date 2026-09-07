namespace Milvus.Client.V2.Responses.Index;

/// <summary>
/// Represents the result of a <c>DescribeIndex</c> operation.
/// </summary>
public sealed class DescribeIndexResp
{
    internal DescribeIndexResp(IReadOnlyList<IndexDescription> indexes)
    {
        Indexes = indexes;
    }

    internal static DescribeIndexResp FromGrpc(Grpc.DescribeIndexResponse response)
        => new(response.IndexDescriptions.Select(IndexDescription.FromGrpc).ToList());

    /// <summary>
    /// The descriptions of the indexes on the field.
    /// </summary>
    public IReadOnlyList<IndexDescription> Indexes { get; }
}
