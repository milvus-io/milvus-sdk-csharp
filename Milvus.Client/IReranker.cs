using System.Globalization;

namespace Milvus.Client;

/// <summary>
/// Interface for rerankers used in hybrid search to combine results from multiple vector searches.
/// </summary>
public interface IReranker
{
    internal IEnumerable<Grpc.KeyValuePair> ToRankParams();
}

/// <summary>
/// Reciprocal Rank Fusion (RRF) reranker that combines results based on their reciprocal ranks.
/// </summary>
/// <remarks>
/// RRF is a popular technique for combining ranked lists. The score for each document is computed as:
/// <c>score = sum(1 / (k + rank))</c> across all input searches, where <c>k</c> is a constant (default 60).
/// </remarks>
public sealed class RrfReranker : IReranker
{
    /// <summary>
    /// Creates a new RRF reranker with the default k value of 60.
    /// </summary>
    public RrfReranker() : this(60)
    {
    }

    /// <summary>
    /// Creates a new RRF reranker with a specified k value.
    /// </summary>
    /// <param name="k">The constant used in the RRF formula. Must be greater than or equal to 1.</param>
    public RrfReranker(float k)
    {
        if (k < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(k), k, "k must be greater than or equal to 1");
        }

        K = k;
    }

    /// <summary>
    /// The constant used in the RRF formula. Higher values give more weight to lower-ranked results.
    /// </summary>
    public float K { get; }

    IEnumerable<Grpc.KeyValuePair> IReranker.ToRankParams()
    {
        yield return new Grpc.KeyValuePair
        {
            Key = "strategy",
            Value = "rrf"
        };
        yield return new Grpc.KeyValuePair
        {
            Key = "params",
            Value = $"{{\"k\": {K.ToString(CultureInfo.InvariantCulture)}}}"
        };
    }
}

/// <summary>
/// Weighted reranker that combines results using specified weights for each input search.
/// </summary>
/// <remarks>
/// The final score for each document is computed as a weighted sum of its scores from each input search.
/// </remarks>
public sealed class WeightedReranker : IReranker
{
    /// <summary>
    /// Creates a new weighted reranker with the specified weights.
    /// </summary>
    /// <param name="weights">
    /// The weights to apply to each input search. The number of weights must match the number of ANN search requests.
    /// </param>
    public WeightedReranker(params float[] weights)
    {
        Verify.NotNull(weights);
        if (weights.Length == 0)
        {
            throw new ArgumentException("At least one weight must be provided", nameof(weights));
        }

        Weights = weights;
    }

    /// <summary>
    /// The weights to apply to each input search. Must have the same number of elements as there are input searches.
    /// </summary>
    public IReadOnlyList<float> Weights { get; }

    IEnumerable<Grpc.KeyValuePair> IReranker.ToRankParams()
    {
        yield return new Grpc.KeyValuePair
        {
            Key = "strategy",
            Value = "weighted"
        };
        yield return new Grpc.KeyValuePair
        {
            Key = "params",
            Value = $"{{\"weights\": [{string.Join(", ", Weights.Select(w => w.ToString(CultureInfo.InvariantCulture)))}]}}"
        };
    }
}
