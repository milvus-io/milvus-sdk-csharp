namespace Milvus.Client.V2.Types;

/// <summary>
/// Aggregated count/latency metrics of a set of operations, as reported by
/// <c>GetClientTelemetryAsync</c>. Mirrors the proto <c>common.Metrics</c>.
/// </summary>
public sealed class MetricsInfo
{
    internal MetricsInfo(
        long requestCount, long successCount, long errorCount,
        double avgLatencyMs, double p99LatencyMs, double maxLatencyMs)
    {
        RequestCount = requestCount;
        SuccessCount = successCount;
        ErrorCount = errorCount;
        AvgLatencyMs = avgLatencyMs;
        P99LatencyMs = p99LatencyMs;
        MaxLatencyMs = maxLatencyMs;
    }

    /// <summary>
    /// The number of requests observed.
    /// </summary>
    public long RequestCount { get; }

    /// <summary>
    /// The number of successful requests.
    /// </summary>
    public long SuccessCount { get; }

    /// <summary>
    /// The number of failed requests.
    /// </summary>
    public long ErrorCount { get; }

    /// <summary>
    /// The average request latency in milliseconds.
    /// </summary>
    public double AvgLatencyMs { get; }

    /// <summary>
    /// The p99 request latency in milliseconds.
    /// </summary>
    public double P99LatencyMs { get; }

    /// <summary>
    /// The maximum request latency in milliseconds.
    /// </summary>
    public double MaxLatencyMs { get; }

    internal static MetricsInfo FromGrpc(Grpc.Metrics? metrics)
    {
        if (metrics is null)
        {
            return new MetricsInfo(0, 0, 0, 0, 0, 0);
        }

        return new MetricsInfo(
            metrics.RequestCount,
            metrics.SuccessCount,
            metrics.ErrorCount,
            metrics.AvgLatencyMs,
            metrics.P99LatencyMs,
            metrics.MaxLatencyMs);
    }
}

/// <summary>
/// The metrics of a single operation type, both globally and per collection. Mirrors the proto
/// <c>common.OperationMetrics</c>.
/// </summary>
public sealed class OperationMetricsInfo
{
    internal OperationMetricsInfo(
        string operation, MetricsInfo global, IReadOnlyDictionary<string, MetricsInfo> collectionMetrics)
    {
        Operation = operation;
        Global = global;
        CollectionMetrics = collectionMetrics;
    }

    /// <summary>
    /// The operation name, e.g. <c>"Search"</c> or <c>"Insert"</c>.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// The operation's metrics aggregated across all collections.
    /// </summary>
    public MetricsInfo Global { get; }

    /// <summary>
    /// The operation's metrics per collection, keyed by collection name.
    /// </summary>
    public IReadOnlyDictionary<string, MetricsInfo> CollectionMetrics { get; }

    internal static OperationMetricsInfo FromGrpc(Grpc.OperationMetrics? metrics)
    {
        if (metrics is null)
        {
            return new OperationMetricsInfo("", MetricsInfo.FromGrpc(null), new Dictionary<string, MetricsInfo>());
        }

        return new OperationMetricsInfo(
            metrics.Operation,
            MetricsInfo.FromGrpc(metrics.Global),
            metrics.CollectionMetrics.ToDictionary(
                pair => pair.Key,
                pair => MetricsInfo.FromGrpc(pair.Value),
                StringComparer.Ordinal));
    }
}
