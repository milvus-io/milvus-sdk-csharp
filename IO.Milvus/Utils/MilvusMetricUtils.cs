namespace IO.Milvus.Utils;

/// <summary>
/// Utils methods for <see cref="MilvusMetricType"/>
/// </summary>
public static class MilvusMetricUtils
{
    /// <summary>
    /// Checks if a metric is for float vector.
    /// </summary>
    /// <param name="metric">Metric type</param>
    /// <returns></returns>
    public static bool IsFloatMetric(this MilvusMetricType metric)
        => metric is MilvusMetricType.L2 or MilvusMetricType.IP;

    /// <summary>
    /// Checks if a metric is for binary vector.
    /// </summary>
    /// <param name="metric">Metric type</param>
    /// <returns></returns>
    public static bool IsBinaryMetric(this MilvusMetricType metric)
        => !IsFloatMetric(metric);
}
