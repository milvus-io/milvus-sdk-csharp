using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get the persistent segment info of a collection.
/// </summary>
public sealed class GetPersistentSegmentInfoReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection whose persistent segment info to retrieve.
    /// </summary>
    public string CollectionName { get; set; } = "";
    internal Grpc.GetPersistentSegmentInfoRequest ToGrpcGetPersistentSegmentInfoRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        return new Grpc.GetPersistentSegmentInfoRequest
        {
            DbName = DatabaseName ?? "",
            CollectionName = CollectionName
        };
    }
}
