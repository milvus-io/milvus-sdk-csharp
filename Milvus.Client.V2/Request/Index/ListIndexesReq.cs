using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to list the indexes of a collection.
/// </summary>
public sealed class ListIndexesReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional field name to filter the listed indexes to those built on that field. When empty, all
    /// indexes of the collection are returned.
    /// </summary>
    public string? FieldName { get; set; }

    internal Grpc.DescribeIndexRequest ToGrpcDescribeIndexRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.DescribeIndexRequest
        {
            CollectionName = CollectionName,
            FieldName = FieldName ?? ""
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
