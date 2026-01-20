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

/// <summary>
/// Represents an ANN search request for full-text search using BM25.
/// </summary>
/// <remarks>
/// This request type is used for hybrid search that combines BM25 full-text search
/// with dense vector search. The text queries are automatically converted to sparse
/// vectors by the Milvus server using the BM25 function defined in the collection schema.
/// </remarks>
public sealed class TextAnnSearchRequest : AnnSearchRequest
{
    /// <summary>
    /// Creates a new ANN search request for BM25 full-text search.
    /// </summary>
    /// <param name="vectorFieldName">
    /// The name of the sparse vector field (output field of the BM25 function) to search in.
    /// </param>
    /// <param name="texts">The text queries to search for.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    public TextAnnSearchRequest(
        string vectorFieldName,
        IReadOnlyList<string> texts,
        int limit)
        : base(vectorFieldName, SimilarityMetricType.Bm25, limit)
    {
        Verify.NotNull(texts);
        if (texts.Count == 0)
        {
            throw new ArgumentException("At least one text query must be provided", nameof(texts));
        }

        Texts = texts;
    }

    /// <summary>
    /// The text queries to search for.
    /// </summary>
    public IReadOnlyList<string> Texts { get; }
}
