namespace Milvus.Client.V2.Types;

/// <summary>
/// Describes how a field value is applied during a partial upsert.
/// </summary>
/// <remarks>
/// <see cref="FieldPartialUpdateOpType.ArrayAppend" />, <see cref="FieldPartialUpdateOpType.ArrayRemove" />
/// and <see cref="FieldPartialUpdateOpType.PathReplace" /> require a newer Milvus server. Older servers
/// ignore these operations and may apply the supplied value using replace semantics.
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

    /// <summary>
    /// The path for a <see cref="FieldPartialUpdateOpType.PathReplace" /> operation on a struct field. The
    /// path is relative to the parent field and uses <c>[index]</c> to replace one complete struct element or
    /// <c>[index][child]</c> to replace one child of that element. Required for <see cref="FieldPartialUpdateOpType.PathReplace" />.
    /// </summary>
    public string? Path { get; set; }

    internal Grpc.FieldPartialUpdateOp ToGrpc()
    {
        var op = new Grpc.FieldPartialUpdateOp
        {
            FieldName = FieldName,
            Op = (Grpc.FieldPartialUpdateOp.Types.OpType)(int)OpType
        };

        // A Path on a non-PATH_REPLACE op is meaningless and would be silently dropped; reject it up front
        // (mirroring pymilvus's "path is only supported for PATH_REPLACE").
        if (OpType != FieldPartialUpdateOpType.PathReplace && !string.IsNullOrWhiteSpace(Path))
        {
            throw new ArgumentException(
                $"Path is only supported for a {FieldPartialUpdateOpType.PathReplace} op, but op for field " +
                $"'{FieldName}' is {OpType}.", nameof(Path));
        }

        if (OpType == FieldPartialUpdateOpType.PathReplace)
        {
            // The proto requires a path for PATH_REPLACE. The row-data path validates it earlier in
            // RowDataConverter, but the columnar path (ColumnsData) does not, so reject a missing path here to
            // avoid forwarding an empty PATH_REPLACE to the server.
            if (string.IsNullOrWhiteSpace(Path))
            {
                throw new ArgumentException(
                    $"Path is required for a {FieldPartialUpdateOpType.PathReplace} op on field '{FieldName}'.",
                    nameof(Path));
            }

            op.Path = Path;
        }

        return op;
    }
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
    ArrayRemove = 2,

    /// <summary>
    /// Replaces an existing value selected by <see cref="FieldPartialUpdateOp.Path" />. The supported paths are
    /// relative to the field name and use either <c>[index]</c> (replace one complete struct element) or
    /// <c>[index][child]</c> (replace one child of that element) syntax.
    /// </summary>
    PathReplace = 3
}
