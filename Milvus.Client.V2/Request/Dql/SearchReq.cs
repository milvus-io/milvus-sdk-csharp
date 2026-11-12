using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// Represents a request to perform a vector similarity search.
/// </summary>
/// <remarks>
/// Exactly one query-target source must be set: <see cref="Vectors" />, <see cref="SparseVectors" />,
/// <see cref="Float16Vectors" />, <see cref="BFloat16Vectors" />, <see cref="BinaryVectors" />,
/// <see cref="Int8Vectors" />, <see cref="Texts" /> or <see cref="Ids" />. The search limit
/// (<see cref="Limit" />, a.k.a. top-K) and an optional <see cref="SearchParameters.Offset" /> must keep their
/// sum in [1, 16384]. When <see cref="SearchParameters.ConsistencyLevel" /> is unset, the search falls back
/// to the collection's configured level.
/// </remarks>
public sealed class SearchReq
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
    /// The query vectors to search for (dense float vectors).
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
    /// of one query; when set, <see cref="VectorFieldName" /> must name a struct sub-field. Mirrors the Java
    /// SDK's <c>EmbeddingList</c> / C++ <c>EmbeddingList</c>.
    /// </summary>
    public IReadOnlyList<EmbeddingList>? EmbeddingLists { get; set; }

    /// <summary>
    /// The primary keys to search for, as an alternative to query vectors. When set, <see cref="Vectors" />,
    /// <see cref="SparseVectors" /> and <see cref="Float16Vectors" /> must be empty.
    /// </summary>
    public IReadOnlyList<object>? Ids { get; set; }

    /// <summary>
    /// The metric type used to measure the distance between vectors.
    /// </summary>
    public MetricType MetricType { get; set; }

    /// <summary>
    /// The maximum number of results to return, also known as 'topk'.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// The optional search parameters.
    /// </summary>
    public SearchParameters? Parameters { get; set; }

    /// <summary>
    /// The row-level security (RLS) principal for the operation. When set, Milvus applies the row policies of
    /// the collection to this search.
    /// </summary>
    public string? RlsPrincipal { get; set; }

    /// <summary>
    /// Whether the operation should skip row-level security (RLS) enforcement. When <c>null</c>, the server
    /// default applies.
    /// </summary>
    public bool? SkipRls { get; set; }

    /// <summary>
    /// Optional function chains applied to this search (e.g. L1/L2 rerank plans), mirroring pymilvus's / the
    /// Java SDK's <c>function_chains</c> and the proto <c>SearchRequest.function_chains</c>.
    /// </summary>
    public IList<FunctionChain> FunctionChains { get; } = new List<FunctionChain>();

    /// <summary>
    /// An optional group-by aggregation spec, mirroring pymilvus's / the Java SDK's
    /// <c>search_aggregation</c> and the proto <c>SearchRequest.search_aggregation</c>. The aggregated buckets
    /// are returned per query in <see cref="Responses.Dql.SearchResp.AggregationBuckets" />.
    /// </summary>
    public SearchAggregation? SearchAggregation { get; set; }

    internal Grpc.PlaceholderValue ToPlaceholderValue()
    {
        if (SparseVectors is { Count: > 0 })
        {
            var sparsePlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.SparseFloatVector };
            foreach (SparseVector<float> sparseVector in SparseVectors)
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
                // Write explicit little-endian bytes, matching the insert encoders (Float16VectorFieldData
                // etc.), rather than Buffer.BlockCopy which depends on host byte order.
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
                // BFloat16 travels as uint16 little-endian bytes, like Float16.
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
            // Each embedding list is packed into one placeholder value: the vectors' values concatenated in
            // little-endian (float) or raw bytes (binary/int8). nq is the number of embedding lists (the struct
            // elements to score), set by the caller. All vectors in a list share one type, which selects the
            // wire placeholder type (EmbListFloatVector / EmbListBinaryVector / EmbListInt8Vector).
            Grpc.PlaceholderType placeholderType = Grpc.PlaceholderType.EmbListFloatVector;
            foreach (EmbeddingList embeddingList in EmbeddingLists)
            {
                if (embeddingList.BinaryVectors.Count > 0)
                {
                    placeholderType = Grpc.PlaceholderType.EmbListBinaryVector;
                    break;
                }

                if (embeddingList.Int8Vectors.Count > 0)
                {
                    placeholderType = Grpc.PlaceholderType.EmbListInt8Vector;
                    break;
                }

                if (embeddingList.Vectors.Count > 0)
                {
                    placeholderType = Grpc.PlaceholderType.EmbListFloatVector;
                    break;
                }
            }

            // All embedding lists in one search must share the same vector type: the placeholder type is
            // derived from the first non-empty list and applied to every list, so a mixed request would
            // silently mis-encode (a list of the wrong type contributes an empty placeholder value). Reject it
            // up front. A single list must also not mix vector types (the encode switch packs only one
            // category, silently dropping the others). An empty list (no vectors of any type) is allowed and
            // simply contributes no query.
            foreach (EmbeddingList embeddingList in EmbeddingLists)
            {
                int nonEmptyCollections = (embeddingList.Vectors.Count > 0 ? 1 : 0)
                    + (embeddingList.BinaryVectors.Count > 0 ? 1 : 0)
                    + (embeddingList.Int8Vectors.Count > 0 ? 1 : 0);
                if (nonEmptyCollections > 1)
                {
                    throw new ArgumentException(
                        "An embedding list cannot mix float, binary and int8 vectors; each list must carry " +
                        "exactly one vector type.", nameof(EmbeddingLists));
                }

                Grpc.PlaceholderType listType = embeddingList.BinaryVectors.Count > 0
                    ? Grpc.PlaceholderType.EmbListBinaryVector
                    : embeddingList.Int8Vectors.Count > 0
                        ? Grpc.PlaceholderType.EmbListInt8Vector
                        : embeddingList.Vectors.Count > 0
                            ? Grpc.PlaceholderType.EmbListFloatVector
                            : Grpc.PlaceholderType.None;

                if (listType != Grpc.PlaceholderType.None && listType != placeholderType)
                {
                    throw new ArgumentException(
                        "All embedding lists in a search must carry the same vector type; a list cannot mix " +
                        "float, binary and int8 vectors.", nameof(EmbeddingLists));
                }

                // Each list is flattened into one placeholder value that the server splits by the schema
                // dimension, so all vectors within one list must share the same size; otherwise the payload is
                // silently mis-split (or rejected opaquely by the server).
                int? uniformLength = null;
                foreach (ReadOnlyMemory<float> vector in embeddingList.Vectors)
                {
                    if (uniformLength is null)
                    {
                        uniformLength = vector.Length;
                    }
                    else if (vector.Length != uniformLength)
                    {
                        throw new ArgumentException(
                            "All vectors within one embedding list must share the same dimension.",
                            nameof(EmbeddingLists));
                    }
                }

                foreach (ReadOnlyMemory<byte> vector in embeddingList.BinaryVectors)
                {
                    if (uniformLength is null)
                    {
                        uniformLength = vector.Length;
                    }
                    else if (vector.Length != uniformLength)
                    {
                        throw new ArgumentException(
                            "All vectors within one embedding list must share the same dimension.",
                            nameof(EmbeddingLists));
                    }
                }

                foreach (ReadOnlyMemory<sbyte> vector in embeddingList.Int8Vectors)
                {
                    if (uniformLength is null)
                    {
                        uniformLength = vector.Length;
                    }
                    else if (vector.Length != uniformLength)
                    {
                        throw new ArgumentException(
                            "All vectors within one embedding list must share the same dimension.",
                            nameof(EmbeddingLists));
                    }
                }
            }

            var embListPlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = placeholderType };
            foreach (EmbeddingList embeddingList in EmbeddingLists)
            {
                switch (placeholderType)
                {
                    case Grpc.PlaceholderType.EmbListFloatVector:
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
                        break;
                    }

                    case Grpc.PlaceholderType.EmbListBinaryVector:
                        // One placeholder value per embedding list, holding all the list's binary vectors'
                        // bytes concatenated (matching the float branch and pymilvus's flattened encoding).
                        var binaryBytes = new List<byte>();
                        foreach (ReadOnlyMemory<byte> vector in embeddingList.BinaryVectors)
                        {
                            binaryBytes.AddRange(vector.ToArray());
                        }

                        embListPlaceholder.Values.Add(ByteString.CopyFrom(binaryBytes.ToArray()));
                        break;

                    case Grpc.PlaceholderType.EmbListInt8Vector:
                        // One placeholder value per embedding list, holding all the list's int8 vectors.
                        var int8Bytes = new List<byte>();
                        foreach (ReadOnlyMemory<sbyte> vector in embeddingList.Int8Vectors)
                        {
                            foreach (sbyte value in vector.Span)
                            {
                                int8Bytes.Add(unchecked((byte)value));
                            }
                        }

                        embListPlaceholder.Values.Add(ByteString.CopyFrom(int8Bytes.ToArray()));
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported embedding list placeholder type {placeholderType}");
                }
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
            // Write explicit little-endian bytes (matching the FP16 path above and the insert encoders),
            // rather than Buffer.BlockCopy which depends on host byte order.
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
}
