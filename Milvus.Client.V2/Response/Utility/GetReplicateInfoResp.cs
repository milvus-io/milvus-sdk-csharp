using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// Represents the result of a get-replicate-info operation.
/// </summary>
public sealed class GetReplicateInfoResp
{
    private GetReplicateInfoResp(ReplicateCheckpoint? checkpoint, ReplicateCheckpoint? salvageCheckpoint)
    {
        Checkpoint = checkpoint;
        SalvageCheckpoint = salvageCheckpoint;
    }

    internal static GetReplicateInfoResp FromGrpc(Grpc.GetReplicateInfoResponse response)
        => new(
            response.Checkpoint is null ? null : ReplicateCheckpoint.FromGrpc(response.Checkpoint),
            response.SalvageCheckpoint is null ? null : ReplicateCheckpoint.FromGrpc(response.SalvageCheckpoint));

    /// <summary>
    /// The last confirmed replication checkpoint, or <c>null</c> if no message has been replicated yet.
    /// </summary>
    public ReplicateCheckpoint? Checkpoint { get; }

    /// <summary>
    /// The salvage checkpoint captured during force promote: the last synced position of the old primary.
    /// Data salvage tools can use this to dump unsynchronized messages from the old primary.
    /// </summary>
    public ReplicateCheckpoint? SalvageCheckpoint { get; }
}
