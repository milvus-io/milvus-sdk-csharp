namespace Milvus.Client;

/// <summary>
/// Represents an Approximate Nearest Neighbor (ANN) search request for use in hybrid search.
/// </summary>
public abstract class AnnSearchRequest
{
    /// <summary>
    /// Creates a new ANN search request.
    /// </summary>
    protected AnnSearchRequest(string vectorFieldName, SimilarityMetricType metricType, int limit)
    {
        Verify.NotNullOrWhiteSpace(vectorFieldName);
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be at least 1");
        }

        VectorFieldName = vectorFieldName;
        MetricType = metricType;
        Limit = limit;
    }

    /// <summary>
    /// The name of the vector field to search.
    /// </summary>
    public string VectorFieldName { get; }

    /// <summary>
    /// The metric type to use for the search.
    /// </summary>
    public SimilarityMetricType MetricType { get; }

    /// <summary>
    /// The maximum number of results to return for this search.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// An optional boolean expression to filter scalar fields before the search.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// Additional search parameters specific to the index type.
    /// </summary>
    public IDictionary<string, string> ExtraParameters { get; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents an ANN search request with dense vectors (float, byte, or Half).
/// </summary>
/// <typeparam name="T">The vector element type (float, byte, or Half).</typeparam>
public sealed class VectorAnnSearchRequest<T> : AnnSearchRequest
{
    /// <summary>
    /// Creates a new ANN search request with dense vectors.
    /// </summary>
    /// <param name="vectorFieldName">The name of the vector field to search.</param>
    /// <param name="vectors">The vectors to search for.</param>
    /// <param name="metricType">The metric type to use for the search.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    public VectorAnnSearchRequest(
        string vectorFieldName,
        IReadOnlyList<ReadOnlyMemory<T>> vectors,
        SimilarityMetricType metricType,
        int limit)
        : base(vectorFieldName, metricType, limit)
    {
        Verify.NotNull(vectors);
        if (vectors.Count == 0)
        {
            throw new ArgumentException("At least one vector must be provided", nameof(vectors));
        }

        Vectors = vectors;
    }

    /// <summary>
    /// The vectors to search for.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<T>> Vectors { get; }
}

/// <summary>
/// Represents an ANN search request with sparse float vectors.
/// </summary>
/// <typeparam name="T">The sparse vector value type.</typeparam>
public sealed class SparseVectorAnnSearchRequest<T> : AnnSearchRequest
{
    /// <summary>
    /// Creates a new ANN search request with sparse float vectors.
    /// </summary>
    /// <param name="vectorFieldName">The name of the vector field to search.</param>
    /// <param name="vectors">The sparse vectors to search for.</param>
    /// <param name="metricType">The metric type to use for the search.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    public SparseVectorAnnSearchRequest(
        string vectorFieldName,
        IReadOnlyList<MilvusSparseVector<T>> vectors,
        SimilarityMetricType metricType,
        int limit)
        : base(vectorFieldName, metricType, limit)
    {
        Verify.NotNull(vectors);
        if (vectors.Count == 0)
        {
            throw new ArgumentException("At least one vector must be provided", nameof(vectors));
        }

        Vectors = vectors;
    }

    /// <summary>
    /// The sparse vectors to search for.
    /// </summary>
    public IReadOnlyList<MilvusSparseVector<T>> Vectors { get; }
}
