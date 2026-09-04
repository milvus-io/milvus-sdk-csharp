using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// Represents a request to perform a hybrid search, combining the results of multiple ANN searches with a
/// reranker.
/// </summary>
public sealed class HybridSearchReq
{
    /// <summary>
    /// The name of the collection to search in.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The individual ANN search requests to combine.
    /// </summary>
    public IReadOnlyList<AnnSearchReq> SearchRequests { get; set; } = Array.Empty<AnnSearchReq>();

    /// <summary>
    /// The reranker used to combine the results of the individual searches.
    /// </summary>
    public IReranker Reranker { get; set; } = new RrfReranker();

    /// <summary>
    /// The total number of results to return after reranking.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// The optional hybrid search parameters (partitions, output fields, consistency, etc.).
    /// </summary>
    public SearchParameters? Parameters { get; set; }

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(SearchRequests);
        Verify.NotNull(Reranker);

        if (Limit < 1 || Limit > 16384)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be between 1 and 16384");
        }

        // When an Offset is set, its sum with the limit must stay in [1, 16384] (the server bound), matching the
        // query and plain-search paths; fail fast instead of letting the server reject or mishandle it.
        if (Parameters?.Offset is not null)
        {
            long total = Limit + Parameters.Offset.Value;
            if (total < 1 || total > 16384)
            {
                throw new ArgumentException(
                    $"The sum of Limit and Offset ({total}) must be between 1 and 16384.");
            }
        }

        if (Reranker is WeightedReranker weightedReranker && weightedReranker.Weights.Count != SearchRequests.Count)
        {
            throw new ArgumentException(
                $"WeightedReranker must have the same number of weights ({weightedReranker.Weights.Count}) as search requests ({SearchRequests.Count})",
                nameof(Reranker));
        }
    }
}
