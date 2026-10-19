using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Snapshot;

/// <summary>
/// Represents a request to unpin snapshot data, releasing a pin previously allocated by
/// <see cref="MilvusClientV2.PinSnapshotDataAsync" />.
/// </summary>
public sealed class UnpinSnapshotDataReq
{
    /// <summary>
    /// The ID of the pin to release, as returned by <see cref="MilvusClientV2.PinSnapshotDataAsync" />.
    /// </summary>
    public long PinId { get; set; }

    internal Grpc.UnpinSnapshotDataRequest ToGrpcUnpinSnapshotDataRequest()
    {
        Verify.GreaterThan(PinId, 0);
        return new Grpc.UnpinSnapshotDataRequest { PinId = PinId };
    }
}
