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
    /// The number of milliseconds to wait for the flush-all operation to complete by polling
    /// <see cref="MilvusClientV2.GetFlushAllStateAsync" />. When zero (default), waits until the flush-all
    /// completes with no timeout.
    /// </summary>
    /// <remarks>
    /// This default is a behavior change from earlier builds, where <c>0</c> made <c>FlushAllAsync</c>
    /// return immediately (fire-and-forget): callers who relied on that now block until the flush-all
    /// completes (or until the <see cref="System.Threading.CancellationToken" /> is cancelled). There is no
    /// fire-and-forget mode; use a bounded <see cref="WaitFlushedMs" />
    /// to cap the wait.
    /// </remarks>
    public int WaitFlushedMs { get; set; }

#pragma warning disable CS0612 // FlushAllRequest.db_name is deprecated in the proto but still honored by the server.
    internal Grpc.FlushAllRequest ToGrpcFlushAllRequest() => new() { DbName = DatabaseName ?? "" };
#pragma warning restore CS0612
}
