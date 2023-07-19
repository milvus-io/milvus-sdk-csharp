namespace IO.Milvus.Utils;

/// <summary>
/// Utils methods for <see cref="MilvusSimilarityMetricType"/>
/// </summary>
public static class MilvusMetricUtils
{
    /// <summary>
    /// Checks if a metric is for float vector.
    /// </summary>
    /// <param name="similarityMetric">Metric type</param>
    /// <returns></returns>
    public static bool IsFloatMetric(this MilvusSimilarityMetricType similarityMetric)
        => similarityMetric is MilvusSimilarityMetricType.L2 or MilvusSimilarityMetricType.Ip;

    /// <summary>
    /// Checks if a metric is for binary vector.
    /// </summary>
    /// <param name="similarityMetric">Metric type</param>
    /// <returns></returns>
    public static bool IsBinaryMetric(this MilvusSimilarityMetricType similarityMetric)
        => !IsFloatMetric(similarityMetric);
}
