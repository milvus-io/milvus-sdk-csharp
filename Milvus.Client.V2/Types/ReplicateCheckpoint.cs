namespace Milvus.Client.V2.Types;

/// <summary>
/// A checkpoint describing the last confirmed replicated message of a physical channel.
/// </summary>
public sealed class ReplicateCheckpoint
{
    internal ReplicateCheckpoint(
        string clusterId, string pchannel, string messageId, WalName walName, ulong timeTick)
    {
        ClusterId = clusterId;
        Pchannel = pchannel;
        MessageId = messageId;
        WalName = walName;
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
    /// The WAL implementation that produced the message, as the typed <see cref="WalName" /> (matching
    /// <see cref="MessageID.WalName" />). An unknown/unset WAL reports <see cref="WalName.Unknown" />.
    /// </summary>
    public WalName WalName { get; }

    /// <summary>
    /// The time tick of the last replicated message.
    /// </summary>
    public ulong TimeTick { get; }

    internal static ReplicateCheckpoint FromGrpc(Grpc.ReplicateCheckpoint checkpoint)
        => new(
            checkpoint.ClusterId,
            checkpoint.Pchannel,
            checkpoint.MessageId?.Id ?? "",
            checkpoint.MessageId is null ? WalName.Unknown : (WalName)(int)checkpoint.MessageId.WALName,
            checkpoint.TimeTick);
}
