using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to describe an index.
/// </summary>
public sealed class DescribeIndexReq
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The field name the index is built on.
    /// </summary>
    public string FieldName { get; set; } = "";

    /// <summary>
    /// The index name. Defaults to <c>"_default_idx"</c>.
    /// </summary>
    public string? IndexName { get; set; }

    internal Grpc.DescribeIndexRequest ToGrpcDescribeIndexRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FieldName);

        return new Grpc.DescribeIndexRequest
        {
            CollectionName = CollectionName,
            FieldName = FieldName,
            IndexName = string.IsNullOrEmpty(IndexName) ? Constants.DefaultIndexName : IndexName
        };
    }
}
