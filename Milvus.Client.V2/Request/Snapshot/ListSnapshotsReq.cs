using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Snapshot;

/// <summary>
/// Represents a request to list the snapshots of a collection.
/// </summary>
public sealed class ListSnapshotsReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to list snapshots for. When empty, all snapshots are listed.
    /// </summary>
    public string CollectionName { get; set; } = "";

    internal Grpc.ListSnapshotsRequest ToGrpcListSnapshotsRequest()
    {
        var request = new Grpc.ListSnapshotsRequest { CollectionName = CollectionName };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
