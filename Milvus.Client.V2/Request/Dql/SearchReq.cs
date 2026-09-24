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
    public SimilarityMetricType MetricType { get; set; }

    /// <summary>
    /// The maximum number of results to return, also known as 'topk'.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// The optional search parameters.
    /// </summary>
    public SearchParameters? Parameters { get; set; }

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
