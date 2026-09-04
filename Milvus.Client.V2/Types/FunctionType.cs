namespace Milvus.Client.V2.Types;

/// <summary>
/// Represents the type of a function in a collection schema.
/// </summary>
public enum FunctionType
{
    /// <summary>
    /// The function type is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A BM25 full-text-search function.
    /// </summary>
    Bm25 = 1,

    /// <summary>
    /// A text-embedding function.
    /// </summary>
    TextEmbedding = 2,

    /// <summary>
    /// A reranking function.
    /// </summary>
    Rerank = 3
}
