namespace Milvus.Client.V2.Types;

/// <summary>
/// The name of the write-ahead log (WAL) implementation backing a physical channel.
/// </summary>
public enum WalName
{
    /// <summary>Unknown or unset WAL.</summary>
    Unknown = 0,

    /// <summary>The RocksMQ WAL.</summary>
    RocksMq = 1,

    /// <summary>The Pulsar WAL.</summary>
    Pulsar = 2,

    /// <summary>The Kafka WAL.</summary>
    Kafka = 3,

    /// <summary>The WoodPecker WAL.</summary>
    WoodPecker = 4,

    /// <summary>A test WAL.</summary>
    Test = 999
}

/// <summary>
/// The ID of a message in a Milvus write-ahead log.
/// </summary>
public sealed class MessageID
{
    /// <summary>
    /// Creates a new message ID.
    /// </summary>
    /// <param name="id">The message ID string.</param>
    /// <param name="walName">The name of the WAL implementation that produced the message.</param>
    public MessageID(string id, WalName walName)
    {
        Id = id;
        WalName = walName;
    }

    /// <summary>
    /// The message ID string.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The name of the WAL implementation that produced the message.
    /// </summary>
    public WalName WalName { get; }

    internal Grpc.MessageID ToGrpc()
        => new()
        {
            Id = Id,
            WALName = (Grpc.WALName)(int)WalName
        };

    internal static MessageID FromGrpc(Grpc.MessageID messageID)
        => new(messageID.Id, (WalName)(int)messageID.WALName);
}
