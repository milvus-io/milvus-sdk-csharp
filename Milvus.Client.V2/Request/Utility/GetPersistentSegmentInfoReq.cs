using Milvus.Client.V2.Types;
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

    /// <summary>
    /// The optional segment states to filter by. When empty, the server returns persistent segments of every
    /// state, preserving the legacy behavior.
    /// </summary>
    public IReadOnlyList<SegmentState>? States { get; set; }

    internal Grpc.GetPersistentSegmentInfoRequest ToGrpcGetPersistentSegmentInfoRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        var request = new Grpc.GetPersistentSegmentInfoRequest
        {
            DbName = DatabaseName ?? "",
            CollectionName = CollectionName
        };
        if (States is { Count: > 0 })
        {
            request.States.AddRange(States.Select(s => (Grpc.SegmentState)(int)s));
        }

        return request;
    }
}
