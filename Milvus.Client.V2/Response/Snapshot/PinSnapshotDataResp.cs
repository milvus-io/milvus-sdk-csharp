namespace Milvus.Client.V2.Responses.Snapshot;

/// <summary>
/// The result of a <c>PinSnapshotData</c> operation, carrying the ID of the allocated pin.
/// </summary>
public sealed class PinSnapshotDataResp
{
    private PinSnapshotDataResp(long pinId) => PinId = pinId;

    internal static PinSnapshotDataResp FromGrpc(Grpc.PinSnapshotDataResponse response)
        => new(response.PinId);

    /// <summary>
    /// The ID of the pin, used to release it via <see cref="MilvusClientV2.UnpinSnapshotDataAsync" />.
    /// </summary>
    public long PinId { get; }
}
