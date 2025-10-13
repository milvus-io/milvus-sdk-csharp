namespace Milvus.Client;

/// <summary>
/// Popular type of rankers
/// </summary>
public static class RankerType
{
    /// <summary>
    /// Reciprocal Rank Fusion (RRF) Ranker is a reranking strategy for Milvus hybrid search that balances results from
    /// multiple vector search paths based on their ranking positions rather than their raw similarity scores.For more
    /// information refer to
    /// <see href="https://milvus.io/docs/rrf-ranker.md#Mechanism-of-RRF-Ranker">RRF Ranker</see>.
    /// </summary>
    public static readonly string RRF = "rrf";

    /// <summary>
    /// Weighted Ranker intelligently combines and prioritizes results from multiple search paths by assigning different
    /// importance weights to each. For more information refer to
    /// <see href="https://milvus.io/docs/weighted-ranker.md">Weighted Ranker</see>.
    /// </summary>
    public static readonly string Weighted = "weighted";
}
