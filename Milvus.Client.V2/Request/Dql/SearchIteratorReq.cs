using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// Represents a request to iterate over search results in batches using a server-side iterator.
/// </summary>
/// <remarks>
/// The search iterator uses the <c>search_iter_v2</c> server protocol (token based) and requires a Milvus
/// server of version 2.5.2 or later.
/// </remarks>
public sealed class SearchIteratorReq
{
    /// <summary>
    /// The name of the collection to search in.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The name of the vector field to search in.
    /// </summary>
    public string VectorFieldName { get; set; } = "";

    /// <summary>
    /// The query vectors to search for (dense float vectors). Only a single query vector is supported.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<float>> Vectors { get; set; } = Array.Empty<ReadOnlyMemory<float>>();

    /// <summary>
    /// The sparse query vectors to search for. When set, <see cref="Vectors" /> must be empty.
    /// </summary>
    public IReadOnlyList<MilvusSparseVector<float>>? SparseVectors { get; set; }

    /// <summary>
    /// The float16 query vectors to search for. When set, <see cref="Vectors" /> must be empty.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<ushort>>? HalfVectors { get; set; }

    /// <summary>
    /// The metric type used to measure the distance between vectors.
    /// </summary>
    public SimilarityMetricType MetricType { get; set; }

    /// <summary>
    /// The maximum total number of rows to return. <c>0</c> or <c>int.MaxValue</c> means no limit.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// The optional search parameters.
    /// </summary>
    public SearchParameters? Parameters { get; set; }

    /// <summary>
    /// The number of rows to fetch per batch. Defaults to 1000.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(VectorFieldName);

        int vectorInputs = (Vectors.Count > 0 ? 1 : 0)
                           + (SparseVectors is { Count: > 0 } ? 1 : 0)
                           + (HalfVectors is { Count: > 0 } ? 1 : 0);
        if (vectorInputs != 1)
        {
            throw new ArgumentException(
                "Exactly one of Vectors, SparseVectors or HalfVectors must be provided.");
        }

        if (Vectors.Count > 1 || (SparseVectors?.Count ?? 0) > 1 || (HalfVectors?.Count ?? 0) > 1)
        {
            throw new ArgumentException("The search iterator does not support processing multiple vectors simultaneously.");
        }

        if (BatchSize < 1 || BatchSize > 16384)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchSize), BatchSize, "Batch size must be between 1 and 16384");
        }

        if (Parameters?.Offset is not null and not 0)
        {
            throw new ArgumentException("Offset is not supported with a search iterator.", nameof(Parameters));
        }
    }
}
