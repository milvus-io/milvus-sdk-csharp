namespace Milvus.Client;

/// <inheritdoc cref="HybridSearchRequest"/>
/// <typeparam name="T">Type of <see cref="Vector"/>. Only float and byte are supported.</typeparam>
public sealed class HybridSearchRequest<T> : HybridSearchRequest
{
    /// <summary>
    /// Vector to send as input for the similarity search.
    /// </summary>
    public ReadOnlyMemory<T> Vector { get; set; }

    /// <param name="vectorFieldName">
    /// The vector field to use in the request.
    /// </param>
    /// <param name="vector">
    /// Vector to send as input for the similarity search.
    /// </param>
    /// <param name="metricType">
    /// Method used to measure the distance between vectors during search. Must correspond to the metric type specified
    /// when building the index.
    /// </param>
    /// <param name="limit">
    /// The maximum number of records to return, also known as 'topk'. Must be between 1 and 16384.
    /// </param>
    /// <param name="parameters">
    /// Various additional optional parameters to configure the similarity search.
    /// </param>
    public HybridSearchRequest(string vectorFieldName,
        ReadOnlyMemory<T> vector,
        SimilarityMetricType metricType,
        int limit,
        SearchParameters? parameters = null)
    {
        VectorFieldName = vectorFieldName;
        Vector = vector;
        MetricType = metricType;
        Limit = limit;
        Parameters = parameters;
    }
}

/// <summary>
/// A class representing an ANN search request.
/// </summary>
public abstract class HybridSearchRequest
{
    /// <summary>
    /// The vector field to use in the request.
    /// </summary>
    public string VectorFieldName { get; protected set; } = null!;

    /// <summary>
    /// Various additional optional parameters to configure the similarity search.
    /// </summary>
    public SearchParameters? Parameters { get; protected set; }

    /// <summary>
    /// Method used to measure the distance between vectors during search. Must correspond to the metric type specified.
    /// when building the index
    /// </summary>
    public SimilarityMetricType MetricType { get; protected set; }

    /// <summary>
    /// The maximum number of records to return, also known as 'topk'. Must be between 1 and 16384.
    /// </summary>
    public int Limit { get; protected set; }


    /// <summary>
    /// Create search request with float vector
    /// </summary>
    /// <param name="vectorFieldName">
    /// The vector field to use in the request.
    /// </param>
    /// <param name="vector"
    /// >Vector to send as input for the similarity search.
    /// </param>
    /// <param name="metricType">
    /// Method used to measure the distance between vectors during search. Must correspond to the metric type specified
    /// when building the index.
    /// </param>
    /// <param name="limit">
    /// The maximum number of records to return, also known as 'topk'. Must be between 1 and 16384.
    /// </param>
    /// <param name="parameters">Various additional optional parameters to configure the similarity search.
    /// </param>
    public static HybridSearchRequest<float> CreateFLoat(string vectorFieldName,
        ReadOnlyMemory<float> vector,
        SimilarityMetricType metricType,
        int limit,
        SearchParameters? parameters = null) => new(vectorFieldName, vector, metricType, limit, parameters);

    /// <summary>
    /// Create search request with float vector.
    /// </summary>
    /// <param name="vectorFieldName">
    /// The vector field to use in the request.
    /// </param>
    /// <param name="vector">
    /// Vector to send as input for the similarity search.
    /// </param>
    /// <param name="metricType">
    /// Method used to measure the distance between vectors during search. Must correspond to the metric type specified
    /// when building the index.
    /// </param>
    /// <param name="limit">
    /// The maximum number of records to return, also known as 'topk'. Must be between 1 and 16384.
    /// </param>
    /// <param name="parameters">
    /// Various additional optional parameters to configure the similarity search.
    /// </param>
    public static HybridSearchRequest<byte> CreateByte(string vectorFieldName,
        ReadOnlyMemory<byte> vector,
        SimilarityMetricType metricType,
        int limit,
        SearchParameters? parameters = null) => new(vectorFieldName, vector, metricType, limit, parameters);
}
