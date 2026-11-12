namespace Milvus.Client.V2.Types;

/// <summary>
/// A list of vectors used to search an Array-of-Struct vector sub-field (e.g. <c>"clips[vector]"</c> with a
/// MAX_SIM metric): each vector in the list is matched against the struct elements, and the struct is scored by
/// the closest element. Mirrors the Java SDK's <c>EmbeddingList</c> and the C++ <c>EmbeddingList</c>. The vectors
/// in one list must all share the same type: float, binary, or int8 (use <see cref="Add" />,
/// <see cref="AddBinary" /> or <see cref="AddInt8" /> accordingly).
/// </summary>
public sealed class EmbeddingList
{
    private readonly List<ReadOnlyMemory<float>> _vectors = [];
    private readonly List<ReadOnlyMemory<byte>> _binaryVectors = [];
    private readonly List<ReadOnlyMemory<sbyte>> _int8Vectors = [];

    /// <summary>
    /// Creates an empty embedding list.
    /// </summary>
    public EmbeddingList()
    {
    }

    /// <summary>
    /// Creates an embedding list from the given float vectors.
    /// </summary>
    public EmbeddingList(IEnumerable<ReadOnlyMemory<float>> vectors)
    {
        Milvus.Client.V2.Utils.Verify.NotNull(vectors);
        _vectors.AddRange(vectors);
    }

    /// <summary>
    /// Adds a float vector to the list.
    /// </summary>
    public EmbeddingList Add(ReadOnlyMemory<float> vector)
    {
        _vectors.Add(vector);
        return this;
    }

    /// <summary>
    /// Adds a binary vector to the list.
    /// </summary>
    public EmbeddingList AddBinary(ReadOnlyMemory<byte> vector)
    {
        _binaryVectors.Add(vector);
        return this;
    }

    /// <summary>
    /// Adds an int8 vector to the list.
    /// </summary>
    public EmbeddingList AddInt8(ReadOnlyMemory<sbyte> vector)
    {
        _int8Vectors.Add(vector);
        return this;
    }

    /// <summary>
    /// The float vectors in the list (empty when the list carries binary or int8 vectors).
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<float>> Vectors => _vectors;

    /// <summary>
    /// The binary vectors in the list (empty when the list carries float or int8 vectors).
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> BinaryVectors => _binaryVectors;

    /// <summary>
    /// The int8 vectors in the list (empty when the list carries float or binary vectors).
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<sbyte>> Int8Vectors => _int8Vectors;

    /// <summary>
    /// The number of vectors in the list.
    /// </summary>
    public int Count => _vectors.Count + _binaryVectors.Count + _int8Vectors.Count;
}
