using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Utility;

/// <summary>
/// Represents a request to flush the given collections, sealing their in-memory segments.
/// </summary>
public sealed class FlushReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The names of the collections to flush.
    /// </summary>
    public IReadOnlyList<string> CollectionNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The maximum time in milliseconds to wait for the flush to complete. When zero (default), the flush waits
    /// forever until all segments are flushed. When greater than zero and the deadline elapses before all
    /// segments are flushed, <c>FlushAsync</c> throws <see cref="TimeoutException" />.
    /// </summary>
    public long WaitFlushedMs { get; set; }

    internal Grpc.FlushRequest ToGrpcFlushRequest()
    {
        Verify.NotNullOrEmpty(CollectionNames);
        var request = new Grpc.FlushRequest { DbName = DatabaseName ?? "" };
        request.CollectionNames.AddRange(CollectionNames);
        return request;
    }
}
