using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Snapshot;

/// <summary>
/// Represents a request to pin snapshot data to prevent garbage collection while copying it out (e.g. during
/// backup).
/// </summary>
public sealed class PinSnapshotDataReq
{
    /// <summary>
    /// The name of the snapshot to pin.
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

    /// <summary>
    /// The time-to-live of the pin in seconds. 0 means no expiry (uses the server default).
    /// </summary>
    public long TtlSeconds { get; set; }

    internal Grpc.PinSnapshotDataRequest ToGrpcPinSnapshotDataRequest()
    {
        Verify.NotNullOrWhiteSpace(SnapshotName);
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.GreaterThanOrEqualTo(TtlSeconds, 0);

        var request = new Grpc.PinSnapshotDataRequest
        {
            Name = SnapshotName,
            CollectionName = CollectionName,
            TtlSeconds = TtlSeconds
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }
}
