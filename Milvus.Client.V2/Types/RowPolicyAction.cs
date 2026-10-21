namespace Milvus.Client.V2.Types;

/// <summary>
/// The operation action governed by a row policy.
/// </summary>
/// <remarks>
/// Mirrors the Java SDK's <c>RowPolicyAction</c> enum and the proto <c>RowPolicyAction</c> values.
/// Values 0-4 are the original public RLS contract and must not be renumbered.
/// </remarks>
public enum RowPolicyAction
{
    /// <summary>
    /// Unknown action. Returned when the server reports an action code that has no mapped value.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Query action.
    /// </summary>
    Query = 0,

    /// <summary>
    /// Search action.
    /// </summary>
    Search = 1,

    /// <summary>
    /// Insert action.
    /// </summary>
    Insert = 2,

    /// <summary>
    /// Delete action.
    /// </summary>
    Delete = 3,

    /// <summary>
    /// Upsert action.
    /// </summary>
    Upsert = 4,

    /// <summary>
    /// Query iterator action.
    /// </summary>
    QueryIterator = 5,

    /// <summary>
    /// Search iterator action.
    /// </summary>
    SearchIterator = 6,

    /// <summary>
    /// Hybrid search action.
    /// </summary>
    HybridSearch = 7
}
