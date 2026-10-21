namespace Milvus.Client.V2.Types;

/// <summary>
/// The type of a row policy.
/// </summary>
/// <remarks>
/// Mirrors the Java SDK's <c>RowPolicyType</c> enum and the proto <c>RowPolicyType</c> values.
/// </remarks>
public enum RowPolicyType
{
    /// <summary>
    /// Unknown policy type. Returned when the server reports a type that has no mapped value.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Permissive policy type. A row matches when any permissive policy applies.
    /// </summary>
    Permissive = 1,

    /// <summary>
    /// Restrictive policy type. A row must match all restrictive policies.
    /// </summary>
    Restrictive = 2
}
