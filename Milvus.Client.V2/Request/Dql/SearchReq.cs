using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// Represents a request to perform a vector similarity search.
/// </summary>
public sealed class SearchReq
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
    public IReadOnlyList<ReadOnlyMemory<ushort>>? HalfVectors { get; set; }

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

        if (HalfVectors is { Count: > 0 })
        {
            var halfPlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.Float16Vector };
            foreach (ReadOnlyMemory<ushort> vector in HalfVectors)
            {
                byte[] bytes = new byte[vector.Length * sizeof(ushort)];
                Buffer.BlockCopy(vector.ToArray(), 0, bytes, 0, bytes.Length);
                halfPlaceholder.Values.Add(ByteString.CopyFrom(bytes));
            }

            return halfPlaceholder;
        }

        var placeholder = new Grpc.PlaceholderValue { Tag = "$0", Type = Grpc.PlaceholderType.FloatVector };

        foreach (ReadOnlyMemory<float> vector in Vectors)
        {
            byte[] bytes = new byte[vector.Length * sizeof(float)];
            Buffer.BlockCopy(vector.Span.ToArray(), 0, bytes, 0, bytes.Length);
            placeholder.Values.Add(ByteString.CopyFrom(bytes));
        }

        return placeholder;
    }
}
