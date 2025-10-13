namespace Milvus.Client;

/// <summary>
/// Milvus function type.
/// </summary>
public enum FunctionType
{
    /// <summary>
    /// Unknown function.
    /// </summary>
    Unknown = Grpc.FunctionType.Unknown,

    /// <summary>
    /// Generates sparse vectors based on the BM25 ranking algorithm from a VARCHAR field.
    /// </summary>
    Bm25 = Grpc.FunctionType.Bm25,

    /// <summary>
    /// Generates dense vectors that capture semantic meaning from a VARCHAR field.
    /// </summary>
    TextEmbedding = Grpc.FunctionType.TextEmbedding,

    /// <summary>
    /// Applies reranking strategies to the search results.
    /// </summary>
    Rerank = Grpc.FunctionType.Rerank
}
