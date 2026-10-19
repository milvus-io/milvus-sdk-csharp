namespace Milvus.Client.V2.Responses.Snapshot;

/// <summary>
/// The result of a <c>ListSnapshots</c> operation.
/// </summary>
public sealed class ListSnapshotsResp
{
    private ListSnapshotsResp(IReadOnlyList<string> snapshots) => Snapshots = snapshots;

    internal static ListSnapshotsResp FromGrpc(Grpc.ListSnapshotsResponse response)
        => new(response.Snapshots.ToList());

    /// <summary>
    /// The names of the listed snapshots.
    /// </summary>
    public IReadOnlyList<string> Snapshots { get; }
}
