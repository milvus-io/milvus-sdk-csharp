namespace Milvus.Client.V2.Types;

/// <summary>
/// A list of vectors used to search an Array-of-Struct vector sub-field (e.g. <c>"clips[vector]"</c> with a
/// MAX_SIM metric): each vector in the list is matched against the struct elements, and the struct is scored by
/// the closest element. Mirrors the Java SDK's <c>EmbeddingList</c> and the C++ <c>EmbeddingList</c>.
/// </summary>
public sealed class EmbeddingList
{
    private readonly List<ReadOnlyMemory<float>> _vectors = [];

    /// <summary>
    /// Creates an empty embedding list.
    /// </summary>
    public EmbeddingList()
    {
    }

    /// <summary>
    /// Creates an embedding list from the given vectors.
    /// </summary>
    public EmbeddingList(IEnumerable<ReadOnlyMemory<float>> vectors)
    {
        Milvus.Client.V2.Utils.Verify.NotNull(vectors);
        _vectors.AddRange(vectors);
    }

    /// <summary>
    /// Adds a vector to the list.
    /// </summary>
    public EmbeddingList Add(ReadOnlyMemory<float> vector)
    {
        _vectors.Add(vector);
        return this;
    }

    /// <summary>
    /// The vectors in the list.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<float>> Vectors => _vectors;

    /// <summary>
    /// The number of vectors in the list.
    /// </summary>
    public int Count => _vectors.Count;
}
