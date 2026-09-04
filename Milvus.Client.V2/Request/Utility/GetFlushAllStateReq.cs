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
#pragma warning disable CS0612 // The server marks FlushAllTs as deprecated but still populates it.
    internal Grpc.GetFlushAllStateRequest ToGrpcGetFlushAllStateRequest()
        => new()
        {
            DbName = DatabaseName ?? "",
            FlushAllTs = FlushAllTimestamp
        };
#pragma warning restore CS0612
}
