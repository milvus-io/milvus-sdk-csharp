using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

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

    internal abstract Grpc.PlaceholderValue CreatePlaceholderValue();

    internal Grpc.SearchRequest ToGrpcSearchRequest(string collectionName)
    {
        var placeholderValue = CreatePlaceholderValue();

        var request = new Grpc.SearchRequest
        {
            CollectionName = collectionName,
            DslType = Grpc.DslType.BoolExprV1,
            PlaceholderGroup = new Grpc.PlaceholderGroup { Placeholders = { placeholderValue } }.ToByteString()
        };

        if (Expression is not null)
        {
            request.Dsl = Expression;
        }

        request.SearchParams.AddRange(new[]
        {
            new Grpc.KeyValuePair { Key = "anns_field", Value = VectorFieldName },
            new Grpc.KeyValuePair { Key = "topk", Value = Limit.ToString(CultureInfo.InvariantCulture) },
            new Grpc.KeyValuePair { Key = "metric_type", Value = MetricType.ToString().ToUpperInvariant() },
            new Grpc.KeyValuePair { Key = "params", Value = CombineParameters(ExtraParameters) }
        });

        return request;
    }

    private static string CombineParameters(IDictionary<string, string> parameters)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append('{');

        int index = 0;
        foreach (var parameter in parameters)
        {
            stringBuilder
                .Append('"')
                .Append(parameter.Key)
                .Append("\":")
                .Append(parameter.Value);

            if (index++ != parameters.Count - 1)
            {
                stringBuilder.Append(", ");
            }
        }

        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }
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

    internal override Grpc.PlaceholderValue CreatePlaceholderValue()
    {
        var placeholderValue = new Grpc.PlaceholderValue { Tag = "$0" };

        switch (Vectors)
        {
            case IReadOnlyList<ReadOnlyMemory<float>> floatVectors:
                PopulateFloatVectorData(floatVectors, placeholderValue);
                break;
            case IReadOnlyList<ReadOnlyMemory<byte>> binaryVectors:
                PopulateBinaryVectorData(binaryVectors, placeholderValue);
                break;
#if NET8_0_OR_GREATER
            case IReadOnlyList<ReadOnlyMemory<Half>> float16Vectors:
                PopulateFloat16VectorData(float16Vectors, placeholderValue);
                break;
#endif
            default:
                throw new ArgumentException("Only vectors of float, byte, or Half are supported");
        }

        return placeholderValue;
    }

    private static void PopulateFloatVectorData(
        IReadOnlyList<ReadOnlyMemory<float>> vectors, Grpc.PlaceholderValue placeholderValue)
    {
        placeholderValue.Type = Grpc.PlaceholderType.FloatVector;

        foreach (var vector in vectors)
        {
#if NET8_0_OR_GREATER
            if (BitConverter.IsLittleEndian)
            {
                placeholderValue.Values.Add(ByteString.CopyFrom(MemoryMarshal.AsBytes(vector.Span)));
                continue;
            }

            int length = vector.Length * sizeof(float);
            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);

            for (int i = 0; i < vector.Length; i++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), vector.Span[i]);
            }

            placeholderValue.Values.Add(ByteString.CopyFrom(bytes.AsSpan(0, length)));
            ArrayPool<byte>.Shared.Return(bytes);
#else
            int length = vector.Length * sizeof(float);
            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);

            for (int i = 0; i < vector.Length; i++)
            {
                float f = vector.Span[i];
                byte[] floatBytes = BitConverter.GetBytes(f);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(floatBytes);
                }
                floatBytes.CopyTo(bytes, i * sizeof(float));
            }

            placeholderValue.Values.Add(ByteString.CopyFrom(bytes.AsSpan(0, length)));
            ArrayPool<byte>.Shared.Return(bytes);
#endif
        }
    }

    private static void PopulateBinaryVectorData(
        IReadOnlyList<ReadOnlyMemory<byte>> vectors, Grpc.PlaceholderValue placeholderValue)
    {
        placeholderValue.Type = Grpc.PlaceholderType.BinaryVector;

        foreach (var vector in vectors)
        {
            placeholderValue.Values.Add(ByteString.CopyFrom(vector.Span));
        }
    }

#if NET8_0_OR_GREATER
    private static void PopulateFloat16VectorData(
        IReadOnlyList<ReadOnlyMemory<Half>> vectors, Grpc.PlaceholderValue placeholderValue)
    {
        placeholderValue.Type = Grpc.PlaceholderType.Float16Vector;

        foreach (var vector in vectors)
        {
            int length = vector.Length * sizeof(ushort);
            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);

            for (int i = 0; i < vector.Length; i++)
            {
                ushort halfBits = BitConverter.HalfToUInt16Bits(vector.Span[i]);
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * sizeof(ushort)), halfBits);
            }

            placeholderValue.Values.Add(ByteString.CopyFrom(bytes.AsSpan(0, length)));
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }
#endif
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

    internal override Grpc.PlaceholderValue CreatePlaceholderValue()
    {
        var placeholderValue = new Grpc.PlaceholderValue
        {
            Tag = "$0",
            Type = Grpc.PlaceholderType.SparseFloatVector
        };

        foreach (var sparseVector in Vectors)
        {
            placeholderValue.Values.Add(ByteString.CopyFrom(sparseVector.ToBytes()));
        }

        return placeholderValue;
    }
}
