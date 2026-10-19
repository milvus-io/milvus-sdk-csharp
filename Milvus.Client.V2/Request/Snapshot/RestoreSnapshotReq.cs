using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Snapshot;

/// <summary>
/// Represents a request to restore a snapshot into a new (possibly cross-db / cross-collection) collection.
/// </summary>
public sealed class RestoreSnapshotReq
{
    /// <summary>
    /// The name of the snapshot to restore.
    /// </summary>
    public string SnapshotName { get; set; } = "";

    /// <summary>
    /// The name of the source collection the snapshot belongs to.
    /// </summary>
    public string SourceCollectionName { get; set; } = "";

    /// <summary>
    /// The name of the collection to restore into. Validated server-side; an existing collection causes the
    /// restore job to fail.
    /// </summary>
    public string TargetCollectionName { get; set; } = "";

    /// <summary>
    /// The name of the source database where the snapshot lives. When empty, the client's currently selected
    /// database is used.
    /// </summary>
    public string? SourceDbName { get; set; }

    /// <summary>
    /// The name of the target database to restore into. When empty, the client's currently selected database
    /// is used.
    /// </summary>
    public string? TargetDbName { get; set; }

    internal Grpc.RestoreSnapshotRequest ToGrpcRestoreSnapshotRequest()
    {
        Verify.NotNullOrWhiteSpace(SnapshotName);
        Verify.NotNullOrWhiteSpace(SourceCollectionName);
        Verify.NotNullOrWhiteSpace(TargetCollectionName);

        var request = new Grpc.RestoreSnapshotRequest
        {
            Name = SnapshotName,
            CollectionName = SourceCollectionName,
            TargetCollectionName = TargetCollectionName
        };
        request.DbName = SourceDbName ?? "";
        request.TargetDbName = TargetDbName ?? "";
        return request;
    }
}
