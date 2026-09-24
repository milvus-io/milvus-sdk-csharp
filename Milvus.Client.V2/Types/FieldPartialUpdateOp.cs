namespace Milvus.Client.V2.Types;

/// <summary>
/// Describes how a field value is applied during a partial upsert.
/// </summary>
/// <remarks>
/// <see cref="FieldPartialUpdateOpType.ArrayAppend" /> and <see cref="FieldPartialUpdateOpType.ArrayRemove" />
/// require Milvus server v2.6.17 or later. Older servers ignore these operations and may apply the supplied
/// array value using replace semantics.
/// </remarks>
public sealed class FieldPartialUpdateOp
{
    /// <summary>
    /// Creates a per-field partial-update operation.
    /// </summary>
    public FieldPartialUpdateOp(string fieldName, FieldPartialUpdateOpType opType = FieldPartialUpdateOpType.Replace)
    {
        FieldName = fieldName;
        OpType = opType;
    }

    /// <summary>
    /// The name of the field this operation targets. The field must also be present in the upsert payload.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The operation applied to the matching field during the partial upsert.
    /// </summary>
    public FieldPartialUpdateOpType OpType { get; }

    internal Grpc.FieldPartialUpdateOp ToGrpc()
        => new()
        {
            FieldName = FieldName,
            Op = (Grpc.FieldPartialUpdateOp.Types.OpType)(int)OpType
        };
}

/// <summary>
/// The operation applied to a field during a partial upsert.
/// </summary>
public enum FieldPartialUpdateOpType
{
    /// <summary>
    /// Overwrites the field with the new values. The default behavior when no operation targets a field.
    /// </summary>
    Replace = 0,

    /// <summary>
    /// Appends the supplied elements to the tail of the existing array. Requires an array field; the resulting
    /// length must not exceed the field's <c>max_capacity</c>.
    /// </summary>
    ArrayAppend = 1,

    /// <summary>
    /// Removes every occurrence of each supplied element from the existing array. Requires an array field; it is
    /// a no-op when the base array is empty or no element matches.
    /// </summary>
    ArrayRemove = 2
}
