using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Index;

/// <summary>
/// Represents a request to drop an index.
/// </summary>
public sealed class DropIndexReq
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
    /// The field name the index is built on. Optional: Milvus identifies the index by
    /// <see cref="IndexName" /> and ignores the field name, so it only needs to be set when a server
    /// version requires it.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// The index name. When unset, the empty name is sent, which the proxy treats as "all indexes of the
    /// collection" (matching Java/PyMilvus). Only set it to drop a specific named index.
    /// </summary>
    public string? IndexName { get; set; }

    internal Grpc.DropIndexRequest ToGrpcDropIndexRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.DropIndexRequest
        {
            CollectionName = CollectionName,
            IndexName = IndexName ?? ""
        };

        if (!string.IsNullOrEmpty(FieldName))
        {
            request.FieldName = FieldName;
        }

        request.DbName = DatabaseName ?? "";
        return request;
    }
}
