namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a flush-all operation.
/// </summary>
public sealed class FlushAllResp
{
    internal FlushAllResp(
        ulong flushAllTimestamp, IReadOnlyDictionary<string, ulong>? flushAllTss)
    {
        FlushAllTimestamp = flushAllTimestamp;
        FlushAllTss = flushAllTss;
    }

#pragma warning disable CS0612 // The server marks FlushAllTs as deprecated but still populates it.
    internal static FlushAllResp FromGrpc(Grpc.FlushAllResponse response)
    {
        // The per-pchannel flush timestamps live in the flush-all messages' "_tt" property (the WAL message
        // time tick); only send flush_all_tss when every reported pchannel carries one, since the server
        // rejects a request that is missing any pchannel.
        IReadOnlyDictionary<string, ulong>? flushAllTss = null;
        if (response.FlushAllMsgs.Count > 0)
        {
            var map = new Dictionary<string, ulong>(response.FlushAllMsgs.Count);
            foreach (KeyValuePair<string, Grpc.ImmutableMessage> pair in response.FlushAllMsgs)
            {
                if (pair.Value.Properties.TryGetValue("_tt", out string? ts)
                    && ulong.TryParse(ts, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ulong value))
                {
                    map[pair.Key] = value;
                }
            }

            flushAllTss = map.Count == response.FlushAllMsgs.Count ? map : null;
        }

        return new(response.FlushAllTs, flushAllTss);
    }
#pragma warning restore CS0612

    /// <summary>
    /// The timestamp identifying the flush-all operation, used to poll its state.
    /// </summary>
    public ulong FlushAllTimestamp { get; }

    /// <summary>
    /// The per-pchannel flush timestamps reported by the flush-all operation, or <c>null</c> when the server
    /// did not report them (or reported a channel without a timestamp).
    /// </summary>
    public IReadOnlyDictionary<string, ulong>? FlushAllTss { get; }
}
