using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// A single message dumped from a physical channel.
/// </summary>
public sealed class DumpMessageInfo
{
    internal DumpMessageInfo(MessageID? messageId, ReadOnlyMemory<byte> payload, IReadOnlyDictionary<string, string> properties)
    {
        MessageId = messageId;
        Payload = payload;
        Properties = properties;
    }

    /// <summary>
    /// The ID of the message, or <c>null</c> if the message has no ID.
    /// </summary>
    public MessageID? MessageId { get; }

    /// <summary>
    /// The message body.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// The message properties.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; }

    internal static DumpMessageInfo FromGrpc(Grpc.ImmutableMessage message)
        => new(
            message.Id is null ? null : MessageID.FromGrpc(message.Id),
            message.Payload.Memory,
            message.Properties.ToDictionary(kv => kv.Key, kv => kv.Value));
}
