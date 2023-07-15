namespace IO.Milvus.Utils;

/// <summary>
/// Utils methods form <see cref="MilvusIndexType"/>
/// </summary>
public static class MilvusIndexUtils
{
    /// <summary>
    /// Checks if an index type is for vector.
    /// </summary>
    /// <param name="indexType">indexType</param>
    /// <returns></returns>
    public static bool IsVectorIndex(this MilvusIndexType indexType)
        => indexType is not (MilvusIndexType.Invalid or MilvusIndexType.Trie);
}
