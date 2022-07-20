namespace IO.Milvus.Param
{
    /// <summary>
    /// Represents the available metric types.
    /// For more information:   <see href="https://milvus.io/docs/v2.0.0/metric.md">Similarity Metrics</see>
    /// </summary>
    public enum MetricType
    {
        INVALID,
        L2,
        IP,
        // Only supported for binary vectors
        HAMMING,
        JACCARD,
        TANIMOTO,
        SUBSTRUCTURE,
        SUPERSTRUCTURE,
    }
}
