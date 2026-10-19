using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Snapshot;

/// <summary>
/// Represents a request to drop a snapshot.
/// </summary>
public sealed class DropSnapshotReq
{
    /// <summary>
    /// The name of the snapshot to drop.
    /// </summary>
    public string SnapshotName { get; set; } = "";

    /// <summary>
    /// The name of the collection the snapshot belongs to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    internal Grpc.DropSnapshotRequest ToGrpcDropSnapshotRequest()
    {
        Verify.NotNullOrWhiteSpace(SnapshotName);
        Verify.NotNullOrWhiteSpace(CollectionName);

        var request = new Grpc.DropSnapshotRequest
        {
            Name = SnapshotName,
            CollectionName = CollectionName
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
