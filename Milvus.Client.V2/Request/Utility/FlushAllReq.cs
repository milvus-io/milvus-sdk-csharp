namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to flush all collections.
/// </summary>
public sealed class FlushAllReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// When greater than zero, <c>FlushAllAsync</c> waits for the flush-all operation to complete by polling
    /// <see cref="MilvusClientV2.GetFlushAllStateAsync" /> for up to this many milliseconds, throwing on
    /// timeout. When zero (default), the RPC returns immediately. Mirrors the C++ SDK's <c>WaitFlushedMs</c>.
    /// </summary>
    public int WaitFlushedMs { get; set; }

#pragma warning disable CS0612 // FlushAllRequest.db_name is deprecated in the proto but still honored by the server.
    internal Grpc.FlushAllRequest ToGrpcFlushAllRequest() => new() { DbName = DatabaseName ?? "" };
#pragma warning restore CS0612
}
