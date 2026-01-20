namespace Milvus.Client;

/// <summary>
/// The type of function to use in a collection schema.
/// </summary>
public enum FunctionType
{
    /// <summary>
    /// Unknown function type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// BM25 function for full-text search. Automatically generates sparse vectors from text input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The BM25 function requires exactly one VARCHAR input field (with analyzer enabled) and
    /// exactly one SPARSE_FLOAT_VECTOR output field.
    /// </para>
    /// <para>
    /// When using BM25, the sparse vector field is automatically populated during insertion based
    /// on the text content of the input field.
    /// </para>
    /// </remarks>
    Bm25 = 1,

    /// <summary>
    /// Text embedding function for generating dense vectors from text.
    /// </summary>
    TextEmbedding = 2,

    /// <summary>
    /// Rerank function for reordering search results.
    /// </summary>
    Rerank = 3
}
