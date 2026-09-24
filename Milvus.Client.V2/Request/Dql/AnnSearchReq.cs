using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// An individual ANN (nearest-neighbour) sub-request of a <see cref="HybridSearchReq" />, mirroring the C++
/// <c>SubSearchRequest</c> and Java <c>AnnSearchReq</c>. Unlike a standalone <see cref="SearchReq" />, an ANN
/// sub-request only describes one vector field to search: the query vector, per-leg limit, filter expression
/// and metric. Output fields, partitions, consistency and the reranker live on the hybrid-search level.
/// </summary>
public sealed class AnnSearchReq
{
    /// <summary>
    /// The name of the vector field to search in.
    /// </summary>
    public string VectorFieldName { get; set; } = "";

    /// <summary>
    /// The query vectors to search for (dense float vectors).
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<float>> Vectors { get; set; } = Array.Empty<ReadOnlyMemory<float>>();

    /// <summary>
    /// The sparse query vectors to search for. When set, <see cref="Vectors" /> must be empty.
    /// </summary>
    public IReadOnlyList<MilvusSparseVector<float>>? SparseVectors { get; set; }

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
    /// of one query. Mirrors the Java SDK's <c>EmbeddingList</c> / C++ <c>EmbeddingList</c>.
    /// </summary>
    public IReadOnlyList<EmbeddingList>? EmbeddingLists { get; set; }

    /// <summary>
    /// The maximum number of results to return for this leg, also known as 'topk'.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// The metric type used to measure the distance between vectors for this leg. Defaults to
    /// <see cref="SimilarityMetricType.Invalid" />, letting the server infer it from the index.
    /// </summary>
    public SimilarityMetricType MetricType { get; set; }

    /// <summary>
    /// A boolean expression to filter the results of this leg.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// Number of entities to skip for this leg.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// The number of decimal places to round the scores to for this leg.
    /// </summary>
    public long? RoundDecimal { get; set; }

    /// <summary>
    /// The search radius for range searches on binary/float vectors.
    /// </summary>
    public string? Radius { get; set; }

    /// <summary>
    /// The range filter for range searches on binary/float vectors.
    /// </summary>
    public string? RangeFilter { get; set; }

    /// <summary>
    /// The field to group the results of this leg by.
    /// </summary>
    public string? GroupByField { get; set; }

    /// <summary>
    /// The maximum number of results per group for this leg.
    /// </summary>
    public int? GroupSize { get; set; }

    /// <summary>
    /// Whether the group size is strict for this leg.
    /// </summary>
    public bool? StrictGroupSize { get; set; }

    /// <summary>
    /// Whether to ignore the growing segments for this leg.
    /// </summary>
    public bool? IgnoreGrowing { get; set; }

    /// <summary>
    /// The timezone used for this leg, e.g. <c>"+08:00"</c> or <c>"America/New_York"</c>.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// A structured function-score reranker (boost / decay / model) for this leg, mapped to the proto
    /// <c>schema.FunctionScore</c>. When set, it takes precedence over <see cref="Rerank" />.
    /// </summary>
    public IReranker? FunctionScoreReranker { get; set; }

    /// <summary>
    /// The reranker to apply to this leg's results (e.g. <c>"rrf"</c>).
    /// </summary>
    /// <remarks>
    /// The Milvus 2.6 proxy never reads a <c>"rerank"</c> search-param key, so setting this property causes the
    /// request conversion to throw <see cref="ArgumentException" /> when the search is issued. Use an
    /// <see cref="IReranker" /> such as <c>RrfReranker</c> or <c>WeightedReranker</c>, or a function-score
    /// reranker via <see cref="FunctionScoreReranker" />.
    /// </remarks>
    public string? Rerank { get; set; }

    /// <summary>
    /// Additional search parameters passed through to the server for this leg.
    /// </summary>
    public IDictionary<string, string> ExtraParameters { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Named expression-template values used by <see cref="Expression" /> for this leg, for parameterized
    /// filters.
    /// </summary>
    public IDictionary<string, object> FilterTemplates { get; } = new Dictionary<string, object>();

    /// <summary>
    /// The highlighter settings used to highlight matched terms for this leg.
    /// </summary>
    public IDictionary<string, string> Highlighter { get; } = new Dictionary<string, string>();

    /// <summary>
    /// The type of highlighter to apply (lexical or semantic).
    /// </summary>
    public HighlightType? HighlightType { get; set; }

    internal Grpc.PlaceholderValue ToPlaceholderValue()
    {
        if (SparseVectors is { Count: > 0 })
        {
            var sparsePlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.SparseFloatVector };
            foreach (MilvusSparseVector<float> sparseVector in SparseVectors)
            {
                sparsePlaceholder.Values.Add(ByteString.CopyFrom(sparseVector.ToBytes()));
            }

            return sparsePlaceholder;
        }

        if (Float16Vectors is { Count: > 0 })
        {
            var halfPlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.Float16Vector };
            foreach (ReadOnlyMemory<ushort> vector in Float16Vectors)
            {
                var bytes = new byte[vector.Length * sizeof(ushort)];
                for (int i = 0; i < vector.Length; i++)
                {
                    ushort half = vector.Span[i];
                    bytes[i * 2] = (byte)(half & 0xFF);
                    bytes[i * 2 + 1] = (byte)(half >> 8);
                }

                halfPlaceholder.Values.Add(ByteString.CopyFrom(bytes));
            }

            return halfPlaceholder;
        }

        if (BFloat16Vectors is { Count: > 0 })
        {
            var bf16Placeholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.Bfloat16Vector };
            foreach (ReadOnlyMemory<ushort> vector in BFloat16Vectors)
            {
                var bytes = new byte[vector.Length * sizeof(ushort)];
                for (int i = 0; i < vector.Length; i++)
                {
                    ushort half = vector.Span[i];
                    bytes[i * 2] = (byte)(half & 0xFF);
                    bytes[i * 2 + 1] = (byte)(half >> 8);
                }

                bf16Placeholder.Values.Add(ByteString.CopyFrom(bytes));
            }

            return bf16Placeholder;
        }

        if (BinaryVectors is { Count: > 0 })
        {
            var binaryPlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.BinaryVector };
            foreach (ReadOnlyMemory<byte> vector in BinaryVectors)
            {
                binaryPlaceholder.Values.Add(ByteString.CopyFrom(vector.ToArray()));
            }

            return binaryPlaceholder;
        }

        if (Int8Vectors is { Count: > 0 })
        {
            var int8Placeholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.Int8Vector };
            foreach (ReadOnlyMemory<sbyte> vector in Int8Vectors)
            {
                var bytes = new byte[vector.Length];
                for (int i = 0; i < vector.Length; i++)
                {
                    bytes[i] = unchecked((byte)vector.Span[i]);
                }

                int8Placeholder.Values.Add(ByteString.CopyFrom(bytes));
            }

            return int8Placeholder;
        }

        if (EmbeddingLists is { Count: > 0 })
        {
            // Each embedding list is packed into one placeholder value: the vectors' floats concatenated in
            // little-endian. nq is the number of embedding lists (the struct elements to score), set by the caller.
            var embListPlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.EmbListFloatVector };
            foreach (EmbeddingList embeddingList in EmbeddingLists)
            {
                int byteCount = 0;
                foreach (ReadOnlyMemory<float> vector in embeddingList.Vectors)
                {
                    byteCount += vector.Length * sizeof(float);
                }

                var bytes = new byte[byteCount];
                int offset = 0;
                foreach (ReadOnlyMemory<float> vector in embeddingList.Vectors)
                {
                    foreach (float value in vector.Span)
                    {
                        byte[] raw = BitConverter.GetBytes(value);
                        bytes[offset++] = BitConverter.IsLittleEndian ? raw[0] : raw[3];
                        bytes[offset++] = BitConverter.IsLittleEndian ? raw[1] : raw[2];
                        bytes[offset++] = BitConverter.IsLittleEndian ? raw[2] : raw[1];
                        bytes[offset++] = BitConverter.IsLittleEndian ? raw[3] : raw[0];
                    }
                }

                embListPlaceholder.Values.Add(ByteString.CopyFrom(bytes));
            }

            return embListPlaceholder;
        }

        if (Texts is { Count: > 0 })
        {
            var textPlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.VarChar };
            foreach (string text in Texts)
            {
                textPlaceholder.Values.Add(ByteString.CopyFromUtf8(text));
            }

            return textPlaceholder;
        }

        var placeholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.FloatVector };

        foreach (ReadOnlyMemory<float> vector in Vectors)
        {
            var bytes = new byte[vector.Length * sizeof(float)];
            for (int i = 0; i < vector.Length; i++)
            {
                byte[] raw = BitConverter.GetBytes(vector.Span[i]);
                bytes[i * 4] = BitConverter.IsLittleEndian ? raw[0] : raw[3];
                bytes[i * 4 + 1] = BitConverter.IsLittleEndian ? raw[1] : raw[2];
                bytes[i * 4 + 2] = BitConverter.IsLittleEndian ? raw[2] : raw[1];
                bytes[i * 4 + 3] = BitConverter.IsLittleEndian ? raw[3] : raw[0];
            }

            placeholder.Values.Add(ByteString.CopyFrom(bytes));
        }

        return placeholder;
    }

    /// <summary>
    /// Validates the sub-request. Called by <c>HybridSearchAsync</c>.
    /// </summary>
    internal void Validate()
    {
        Verify.NotNullOrWhiteSpace(VectorFieldName);

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
                "Exactly one of Vectors, SparseVectors, Float16Vectors, BFloat16Vectors, BinaryVectors, Int8Vectors, Texts or EmbeddingLists must be provided for each sub-request.");
        }

        if (Limit < 1 || Limit > 16384)
        {
            throw new ArgumentOutOfRangeException(nameof(Limit), Limit, "Limit must be between 1 and 16384");
        }

        if (Offset is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Offset), Offset, "Offset must be non-negative.");
        }

        // Each leg is emitted as a full SearchRequest with topk + offset; the proxy's window check requires
        // their sum in [1, 16384], matching the plain-search and hybrid top-level paths. Fail fast here
        // instead of letting the server reject it.
        if (Offset is { } offset)
        {
            long total = Limit + offset;
            if (total < 1 || total > 16384)
            {
                throw new ArgumentException(
                    $"The sum of Limit and Offset ({total}) must be between 1 and 16384.");
            }
        }
    }
}
