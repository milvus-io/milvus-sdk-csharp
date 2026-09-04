using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// Represents the result of a get-replicate-info operation.
/// </summary>
public sealed class GetReplicateInfoResp
{
    private GetReplicateInfoResp(ReplicateCheckpoint? checkpoint)
    {
        Checkpoint = checkpoint;
    }

    internal static GetReplicateInfoResp FromGrpc(Grpc.GetReplicateInfoResponse response)
        => new(response.Checkpoint is null ? null : ReplicateCheckpoint.FromGrpc(response.Checkpoint));

    /// <summary>
    /// The last confirmed replication checkpoint, or <c>null</c> if no message has been replicated yet.
    /// </summary>
    public ReplicateCheckpoint? Checkpoint { get; }
}
