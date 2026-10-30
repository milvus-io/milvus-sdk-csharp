using Milvus.Client.V2.Responses.Dql;
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
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

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
    public IReadOnlyList<SparseVector<float>>? SparseVectors { get; set; }

    /// <summary>
    /// The float16 query vectors to search for. When set, <see cref="Vectors" /> must be empty.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<ushort>>? Float16Vectors { get; set; }

    /// <summary>
    /// The bfloat16 query vectors to search for. When set, <see cref="Vectors" /> must be empty.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<ushort>>? BFloat16Vectors { get; set; }

    /// <summary>
    /// The binary query vectors to search for (raw bytes). When set, <see cref="Vectors" /> must be empty.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>>? BinaryVectors { get; set; }

    /// <summary>
    /// The int8 query vectors to search for. When set, <see cref="Vectors" /> must be empty.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<sbyte>>? Int8Vectors { get; set; }

    /// <summary>
    /// The text query strings to search for (embedded-text / BM25 targets). When set, <see cref="Vectors" />
    /// must be empty.
    /// </summary>
    public IReadOnlyList<string>? Texts { get; set; }

    /// <summary>
    /// The embedding lists to search for, used for Array-of-Struct vector sub-fields (e.g. searching
    /// <c>"clips[vector]"</c> with a MAX_SIM metric). Each <see cref="EmbeddingList" /> carries the vectors
    /// of one query; when set, <see cref="VectorFieldName" /> must name a struct sub-field. Mirrors the C++
    /// SDK's <c>EmbeddingList</c>. The search iterator accepts a single query, so only one
    /// <see cref="EmbeddingList" /> may be provided.
    /// </summary>
    public IReadOnlyList<EmbeddingList>? EmbeddingLists { get; set; }

    /// <summary>
    /// The metric type used to measure the distance between vectors.
    /// </summary>
    public MetricType MetricType { get; set; }

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

    /// <summary>
    /// The row-level security (RLS) principal for the operation. When set, Milvus applies the row policies of
    /// the collection to this search iterator.
    /// </summary>
    public string? RlsPrincipal { get; set; }

    /// <summary>
    /// Whether the operation should skip row-level security (RLS) enforcement. When <c>null</c>, the server
    /// default applies.
    /// </summary>
    public bool? SkipRls { get; set; }

    /// <summary>
    /// An optional client-side per-page filter, mirroring the Java SDK's <c>SearchIteratorReqV2.externalFilterFunc</c>
    /// and the C++ SDK's <c>SetExternalFilterFunc</c>. The function receives a page's <see cref="SingleResult" />
    /// and returns the 0-based row indices to keep (in the desired order). Rows not listed are dropped from the
    /// page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The index-based contract deliberately diverges from the sibling SDKs: Java/pymilvus return the kept
    /// results (<c>List&lt;SearchResult&gt;</c> / <c>Hits</c>) and C++ mutates the <c>SingleResult</c> in
    /// place. Here <see cref="SingleResult" /> is an immutable DTO that users cannot reconstruct, so the
    /// callback returns the 0-based row indices to keep and the SDK rebuilds the filtered page via
    /// <c>SelectRows</c>. Returning indices (rather than rows) also permits reordering the kept hits.
    /// </para>
    /// <para>
    /// When the filter is set, the iterator accumulates the filtered pages and yields a batch only once it
    /// reaches <see cref="BatchSize" /> rows (or the remaining <see cref="Limit" />, whichever is smaller),
    /// mirroring the Java <c>SearchIteratorV2.next</c> cache and the C++ <c>FetchPageFromCache</c>. A page
    /// that filters to zero rows is skipped and the iterator keeps pulling the next one; the final (possibly
    /// smaller) batch is yielded when the server has no more results.
    /// </para>
    /// </remarks>
    public Func<SingleResult, IReadOnlyList<int>>? ExternalFilterFunc { get; set; }

    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(VectorFieldName);

        // C++ SearchIteratorImpl::CheckInput rejects MetricType::DEFAULT ("must specify metrics type for
        // search iterator"); the C# enum's Invalid member is the equivalent sentinel.
        if (MetricType == MetricType.Invalid)
        {
            throw new ArgumentException(
                "MetricType must be specified for a search iterator.", nameof(MetricType));
        }

        int vectorInputs = (Vectors.Count > 0 ? 1 : 0)
                           + (SparseVectors is { Count: > 0 } ? 1 : 0)
                           + (Float16Vectors is { Count: > 0 } ? 1 : 0)
                           + (BFloat16Vectors is { Count: > 0 } ? 1 : 0)
                           + (BinaryVectors is { Count: > 0 } ? 1 : 0)
                           + (Int8Vectors is { Count: > 0 } ? 1 : 0)
                           + (Texts is { Count: > 0 } ? 1 : 0)
                           + (EmbeddingLists is { Count: > 0 } ? 1 : 0);
        if (vectorInputs != 1)
        {
            throw new ArgumentException(
                "Exactly one of Vectors, SparseVectors, Float16Vectors, BFloat16Vectors, BinaryVectors, Int8Vectors, Texts or EmbeddingLists must be provided.");
        }

        if (Vectors.Count > 1 || (SparseVectors?.Count ?? 0) > 1 || (Float16Vectors?.Count ?? 0) > 1
            || (BFloat16Vectors?.Count ?? 0) > 1 || (BinaryVectors?.Count ?? 0) > 1
            || (Int8Vectors?.Count ?? 0) > 1 || (Texts?.Count ?? 0) > 1 || (EmbeddingLists?.Count ?? 0) > 1)
        {
            throw new ArgumentException("The search iterator does not support processing multiple vectors simultaneously.");
        }

        // Fail fast on an empty inner embedding list (e.g. `new EmbeddingList()` with no vectors), which would
        // otherwise pack a zero-byte EmbListFloatVector placeholder and only fail server-side. Mirrors the Java
        // SDK's EmbeddingList.getData "EmbeddingList is empty" guard.
        if (EmbeddingLists is { Count: > 0 })
        {
            foreach (EmbeddingList embeddingList in EmbeddingLists)
            {
                if (embeddingList.Count == 0)
                {
                    throw new ArgumentException(
                        "An EmbeddingList must contain at least one vector.", nameof(EmbeddingLists));
                }
            }
        }

        if (BatchSize < 1 || BatchSize > 16384)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchSize), BatchSize, "Batch size must be between 1 and 16384");
        }

        if (Limit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be non-negative.");
        }

        if (Parameters?.Offset is not null and not 0)
        {
            throw new ArgumentException("Offset is not supported with a search iterator.", nameof(Parameters));
        }

        // The search iterator uses the search_iter_v2 token protocol, which pages independently of ordering;
        // reject an order-by up front instead of emitting the params on every page -- both the typed property
        // and the raw ExtraParameters escape hatch.
        if (Parameters?.OrderByFields.Count > 0 || Parameters?.ExtraParameters.ContainsKey(Constants.OrderByFields) == true)
        {
            throw new ArgumentException(
                "OrderByFields (typed or via ExtraParameters) is not supported with a search iterator (the " +
                "search_iter_v2 token protocol pages independently of ordering, matching the Rust SDK's " +
                "rejection and the Java/C++ search iterators).",
                nameof(Parameters));
        }
    }
}
