using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to describe an index.
/// </summary>
public sealed class DescribeIndexReq
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
    /// The field name the index is built on. Optional: the proxy forwards only the collection id, index name
    /// and timestamp to the data coordinator and never reads <c>field_name</c>, so leaving it empty describes
    /// all indexes (or the specific <see cref="IndexName" />) of the collection, matching the Java SDK.
    /// </summary>
    public string FieldName { get; set; } = "";

    /// <summary>
    /// The index name. When unset, the empty name is sent, which the proxy treats as "all indexes of the
    /// collection" (matching Java/PyMilvus). Only set it to describe a specific named index.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Only checks index state at this timestamp. All segments are checked when zero.
    /// </summary>
    public ulong Timestamp { get; set; }

    internal Grpc.DescribeIndexRequest ToGrpcDescribeIndexRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.DescribeIndexRequest
        {
            CollectionName = CollectionName,
            FieldName = FieldName,
            IndexName = IndexName ?? "",
            Timestamp = Timestamp
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
