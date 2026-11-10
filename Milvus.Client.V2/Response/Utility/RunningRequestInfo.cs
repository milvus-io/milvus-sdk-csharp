namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// One Search, HybridSearch or Query request that a proxy is executing.
/// </summary>
public sealed class RunningRequestInfo
{
    private RunningRequestInfo(
        string requestId, long proxyId, string type, string databaseName, string collectionName, string user,
        string clientAddr, long nq, long topK, string expr, long startTimeMs, long queuedMs, long elapsedMs,
        string state, IReadOnlyList<long> taskIds, string traceId, bool cancellable)
    {
        RequestId = requestId;
        ProxyId = proxyId;
        Type = type;
        DatabaseName = databaseName;
        CollectionName = collectionName;
        User = user;
        ClientAddr = clientAddr;
        Nq = nq;
        TopK = topK;
        Expr = expr;
        StartTimeMs = startTimeMs;
        QueuedMs = queuedMs;
        ElapsedMs = elapsedMs;
        State = state;
        TaskIds = taskIds;
        TraceId = traceId;
        Cancellable = cancellable;
    }

    internal static RunningRequestInfo FromGrpc(Grpc.RunningRequestInfo info)
        => new(
            info.RequestId, info.ProxyId, info.Type, info.DbName, info.CollectionName, info.User, info.ClientAddr,
            info.Nq, info.Topk, info.Expr, info.StartTimeMs, info.QueuedMs, info.ElapsedMs, info.State,
            info.TaskIds.ToList(), info.TraceId, info.Cancellable);

    /// <summary>
    /// The request id, an opaque string such as <c>"25-to03zu-rs"</c>; pass it to
    /// <see cref="MilvusClientV2.CancelRequestsAsync" /> unchanged.
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// The node id of the proxy that holds the request.
    /// </summary>
    public long ProxyId { get; }

    /// <summary>
    /// The request type: <c>Search</c>, <c>HybridSearch</c> or <c>Query</c>.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The database name.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// The collection name.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The user who issued the request.
    /// </summary>
    public string User { get; }

    /// <summary>
    /// The client address seen by the proxy.
    /// </summary>
    public string ClientAddr { get; }

    /// <summary>
    /// The number of query vectors.
    /// </summary>
    public long Nq { get; }

    /// <summary>
    /// The top-k of the request.
    /// </summary>
    public long TopK { get; }

    /// <summary>
    /// The leading part (at most 256 bytes) of the filter expression.
    /// </summary>
    public string Expr { get; }

    /// <summary>
    /// The time the request started, in milliseconds since the Unix epoch.
    /// </summary>
    public long StartTimeMs { get; }

    /// <summary>
    /// How long the request's first task waited in the proxy queue, in milliseconds; 0 until that task starts.
    /// </summary>
    public long QueuedMs { get; }

    /// <summary>
    /// How long the request has been running, in milliseconds. In a <see cref="MilvusClientV2.CancelRequestsAsync" />
    /// answer it is how long the request had been running when it was canceled.
    /// </summary>
    public long ElapsedMs { get; }

    /// <summary>
    /// The request state: <c>Queued</c>, <c>Running</c> or <c>Canceling</c>. In a
    /// <see cref="MilvusClientV2.CancelRequestsAsync" /> answer it is the state before the cancel, or
    /// <c>Canceling</c> when the call repeated an earlier cancel.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// The ids of the proxy tasks the request has created.
    /// </summary>
    public IReadOnlyList<long> TaskIds { get; }

    /// <summary>
    /// The trace id of the request.
    /// </summary>
    public string TraceId { get; }

    /// <summary>
    /// Whether the request can be canceled.
    /// </summary>
    public bool Cancellable { get; }
}
