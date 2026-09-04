using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to dump CDC messages from a physical channel.
/// </summary>
public sealed class DumpMessagesReq
{
    /// <summary>
    /// The physical channel name to dump from.
    /// </summary>
    public string Pchannel { get; set; } = "";

    /// <summary>
    /// The start position in the WAL (required). Typically taken from the replication checkpoint's message ID.
    /// </summary>
    public MessageID StartMessageId { get; set; } = null!;

    /// <summary>
    /// An optional start timetick filter: only messages with a timetick greater than or equal to this value
    /// are dumped.
    /// </summary>
    public ulong? StartTimetick { get; set; }

    /// <summary>
    /// An optional end timetick filter: only messages with a timetick less than or equal to this value are
    /// dumped. <c>0</c> means no limit (stream until cancelled).
    /// </summary>
    public ulong EndTimetick { get; set; }

    /// <summary>
    /// If <c>true</c>, dumping starts from <see cref="StartMessageId" /> inclusively; otherwise it starts
    /// after it.
    /// </summary>
    public bool IncludeStartMessage { get; set; }

    internal Grpc.DumpMessagesRequest ToGrpcRequest()
    {
        Verify.NotNullOrWhiteSpace(Pchannel);
        Verify.NotNull(StartMessageId);

        return new Grpc.DumpMessagesRequest
        {
            Pchannel = Pchannel,
            StartMessageId = StartMessageId.ToGrpc(),
            StartTimetick = StartTimetick ?? 0,
            EndTimetick = EndTimetick,
            IncludeStartMessage = IncludeStartMessage
        };
    }
}
