namespace Milvus.Client.V2.Types;

/// <summary>
/// A checkpoint describing the last confirmed replicated message of a physical channel.
/// </summary>
public sealed class ReplicateCheckpoint
{
    internal ReplicateCheckpoint(
        string clusterId, string pchannel, string messageId, ulong timeTick)
    {
        ClusterId = clusterId;
        Pchannel = pchannel;
        MessageId = messageId;
        TimeTick = timeTick;
    }

    /// <summary>
    /// The ID of the source cluster.
    /// </summary>
    public string ClusterId { get; }

    /// <summary>
    /// The physical channel of the source cluster.
    /// </summary>
    public string Pchannel { get; }

    /// <summary>
    /// The ID of the last confirmed message of the last replicated message.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// The time tick of the last replicated message.
    /// </summary>
    public ulong TimeTick { get; }

    internal static ReplicateCheckpoint FromGrpc(Grpc.ReplicateCheckpoint checkpoint)
        => new(
            checkpoint.ClusterId,
            checkpoint.Pchannel,
            checkpoint.MessageId?.Id ?? "",
            checkpoint.TimeTick);
}
