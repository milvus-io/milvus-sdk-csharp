using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Snapshot;

/// <summary>
/// Represents a request to create a snapshot of a collection.
/// </summary>
public sealed class CreateSnapshotReq
{
    /// <summary>
    /// The name of the snapshot to create. Must be unique.
    /// </summary>
    public string SnapshotName { get; set; } = "";

    /// <summary>
    /// The name of the collection to snapshot.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// An optional description for the snapshot.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// The duration in seconds during which the segments referenced by this snapshot are protected from
    /// compaction. 0 means no protection.
    /// </summary>
    public long CompactionProtectionSeconds { get; set; }

    internal Grpc.CreateSnapshotRequest ToGrpcCreateSnapshotRequest()
    {
        Verify.NotNullOrWhiteSpace(SnapshotName);
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.GreaterThanOrEqualTo(CompactionProtectionSeconds, 0);

        var request = new Grpc.CreateSnapshotRequest
        {
            Name = SnapshotName,
            CollectionName = CollectionName,
            Description = Description,
            CompactionProtectionSeconds = CompactionProtectionSeconds
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
