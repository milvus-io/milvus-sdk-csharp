namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to get the state of a flush-all operation.
/// </summary>
public sealed class GetFlushAllStateReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The timestamp returned by the flush-all operation, identifying the flush to wait for.
    /// </summary>
    public ulong FlushAllTimestamp { get; set; }

    /// <summary>
    /// Per-pchannel flush timestamps returned by a flush-all operation (proto <c>flush_all_tss</c>, field 5).
    /// When provided, the server verifies the flush per physical channel rather than via the deprecated global
    /// <see cref="FlushAllTimestamp" />.
    /// </summary>
    public IReadOnlyDictionary<string, ulong>? FlushAllTss { get; set; }

#pragma warning disable CS0612 // The server marks FlushAllTs as deprecated but still populates it.
    internal Grpc.GetFlushAllStateRequest ToGrpcGetFlushAllStateRequest()
    {
        var request = new Grpc.GetFlushAllStateRequest
        {
            DbName = DatabaseName ?? "",
            FlushAllTs = FlushAllTimestamp
        };

        if (FlushAllTss is { Count: > 0 })
        {
            foreach (KeyValuePair<string, ulong> pair in FlushAllTss)
            {
                request.FlushAllTss.Add(pair.Key, pair.Value);
            }
        }

        return request;
    }
#pragma warning restore CS0612
}
