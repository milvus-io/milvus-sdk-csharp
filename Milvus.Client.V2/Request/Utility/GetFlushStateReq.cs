using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to check whether the given segments have been flushed.
/// </summary>
public sealed class GetFlushStateReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The segment IDs to check.
    /// </summary>
    public IReadOnlyList<long> SegmentIds { get; set; } = Array.Empty<long>();

    /// <summary>
    /// The collection name the segments belong to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The hybrid flush timestamp of the collection, from <c>FlushResp.CollFlushTs</c>. When set, the datacoord
    /// waits for the WAL channel checkpoint to catch up (cpTs &lt; FlushTs) before reporting flushed; passing 0
    /// skips that gate.
    /// </summary>
    public ulong FlushTs { get; set; }

    internal Grpc.GetFlushStateRequest ToGrpcGetFlushStateRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(SegmentIds);

        var request = new Grpc.GetFlushStateRequest
        {
            DbName = DatabaseName ?? "",
            CollectionName = CollectionName,
            FlushTs = FlushTs
        };
        request.SegmentIDs.AddRange(SegmentIds);
        return request;
    }
}
