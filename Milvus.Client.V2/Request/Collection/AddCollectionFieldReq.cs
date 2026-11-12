using System.Text.RegularExpressions;

using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;
namespace Milvus.Client.V2.Requests.Collection;

/// <summary>
/// Represents a request to add a new field to an existing collection. The field name may also be a struct
/// sub-field path such as <c>clips[note]</c>, which adds a new sub-field to the existing struct field
/// <c>clips</c>.
/// </summary>
public sealed class AddCollectionFieldReq
{
    // Matches a struct sub-field path like "clips[note]" with non-nested, C-style identifier parts,
    // mirroring pymilvus Prepare.struct_field_path and the C++/Rust SDKs.
    private static readonly Regex StructSubFieldPathRegex =
        new(@"^[A-Za-z_][A-Za-z_0-9]*\[[A-Za-z_][A-Za-z_0-9]*\]$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to add the field to.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The schema of the field to add. To add a struct sub-field, set <see cref="FieldSchema.Name" /> to the
    /// bracketed path (e.g. <c>clips[note]</c>), set <see cref="FieldSchema.Nullable" /> to <c>true</c>
    /// (element-level null), and for a nested Array sub-field set <see cref="FieldSchema.ElementDataType" />,
    /// <see cref="FieldSchema.MaxCapacity" /> and optionally <see cref="FieldSchema.ElementNullable" />.
    /// </summary>
    public FieldSchema Field { get; set; } = null!;

    /// <summary>
    /// Whether the field name is a struct sub-field path like <c>clips[note]</c>.
    /// </summary>
    internal bool IsStructSubFieldPath => Field is not null && StructSubFieldPathRegex.IsMatch(Field.Name);

    internal Grpc.AddCollectionFieldRequest ToGrpcAddCollectionFieldRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(Field);

        // The legacy AddCollectionField RPC cannot express a struct sub-field; adding one requires the modern
        // AlterCollectionSchema RPC (aligned with pymilvus and the Java/C++/Rust SDKs). A sub-field path must
        // never reach this legacy conversion.
        if (IsStructSubFieldPath)
        {
            throw new ArgumentException(
                $"Struct sub-field '{Field.Name}' requires the AlterCollectionSchema RPC; the legacy " +
                "AddCollectionField RPC cannot express a sub-field path.", nameof(Field));
        }

        ValidateAddedField();

        var grpcField = CreateCollectionReq.ConvertToGrpcFieldSchema(Field);

        var request = new Grpc.AddCollectionFieldRequest { CollectionName = CollectionName, Schema = grpcField.ToByteString() };
        request.DbName = DatabaseName ?? "";
        return request;
    }

    internal Grpc.AlterCollectionSchemaRequest ToGrpcAlterCollectionSchemaRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNull(Field);

        if (Field.Name.Length == 0)
        {
            throw new ArgumentException("The field name must be specified.", nameof(Field));
        }

        // A bracket-containing name that is not a valid struct sub-field path (e.g. "clips[note" or
        // "clips[note][sub]") is rejected locally, aligned with pymilvus Prepare.struct_field_path.
        if (Field.Name.Contains('[') || Field.Name.Contains(']'))
        {
            if (!IsStructSubFieldPath)
            {
                throw new ArgumentException(
                    $"Invalid struct sub-field path: {Field.Name}; expected struct[sub] with identifier names.",
                    nameof(Field));
            }

            ValidateStructSubField();
        }
        else
        {
            ValidateAddedField();
        }

        var fieldInfo = new Grpc.AlterCollectionSchemaRequest.Types.FieldInfo
        {
            // A struct sub-field add carries only one element's type and is element-nullable; row nullability
            // and the outer capacity are inherited from the parent struct, so the sub-field schema is wrapped
            // with rowNullable=false and no outer capacity (mirroring pymilvus and the Java/C++/Rust SDKs).
            FieldSchema = IsStructSubFieldPath
                ? CreateCollectionReq.ConvertToGrpcStructSubFieldSchema(Field, rowNullable: false, outerCapacity: null)
                : CreateCollectionReq.ConvertToGrpcFieldSchema(Field)
        };

        var addRequest = new Grpc.AlterCollectionSchemaRequest.Types.AddRequest();
        addRequest.FieldInfos.Add(fieldInfo);

        var request = new Grpc.AlterCollectionSchemaRequest
        {
            CollectionName = CollectionName,
            Action = new Grpc.AlterCollectionSchemaRequest.Types.Action
            {
                AddRequest = addRequest
            }
        };
        request.DbName = DatabaseName ?? "";
        return request;
    }

    // Milvus requires every ordinary field added to an existing collection to be nullable, unless it carries a
    // default value: the proxy's validateAddedEvolutionField rejects only nullable==false AND
    // default_value==nil, so a non-nullable field with a default is a valid request.
    private void ValidateAddedField()
    {
        if (!Field.Nullable && Field.DefaultValue is null)
        {
            throw new ArgumentException(
                $"The field '{Field.Name}' must be nullable or have a default value to be added to an existing " +
                "collection (Milvus rejects added fields with nullable=false and no default).", nameof(Field));
        }
    }

    private void ValidateStructSubField()
    {
        // A struct sub-field add must be element-level nullable (aligned with pymilvus and the sibling SDKs).
        if (!Field.Nullable)
        {
            throw new ArgumentException(
                $"Adding a struct sub-field requires nullable=true, field name: {Field.Name}.", nameof(Field));
        }

        if (Field.IsPrimaryKey || Field.IsPartitionKey || Field.IsClusteringKey || Field.DefaultValue is not null)
        {
            throw new ArgumentException(
                $"A struct sub-field cannot be a primary, partition, or clustering key or have a default " +
                $"value, field name: {Field.Name}.", nameof(Field));
        }

        // These checks mirror CreateCollectionReq.ValidateStructSubFieldSchema so the create path and the
        // sub-field-add path reject the same malformed sub-fields up front.
        if (Field.DataType == DataType.SparseFloatVector)
        {
            throw new ArgumentException(
                $"Struct sub-field '{Field.Name}' does not support {nameof(DataType.SparseFloatVector)}: the " +
                "server only accepts fixed-dimension vector element types in a struct.", nameof(Field));
        }

        if (Field.EnableAnalyzer || Field.EnableMatch
            || Field.AnalyzerParams is { Count: > 0 } || Field.MultiAnalyzerParams is { Count: > 0 })
        {
            throw new ArgumentException(
                $"Struct sub-field '{Field.Name}' does not support text analyzers " +
                "(EnableAnalyzer/EnableMatch/AnalyzerParams/MultiAnalyzerParams).", nameof(Field));
        }

        if (Field.DataType == DataType.Array)
        {
            if (Field.ElementDataType is null)
            {
                throw new ArgumentException(
                    $"Element data type is required for Array sub-field '{Field.Name}'.", nameof(Field));
            }

            if (Field.ElementDataType == DataType.Array)
            {
                throw new ArgumentException(
                    $"Struct sub-field '{Field.Name}' does not support nested Array of Array.", nameof(Field));
            }

            if (Field.MaxCapacity is null or <= 0)
            {
                throw new ArgumentException(
                    $"max_capacity is required for Array sub-field '{Field.Name}'.", nameof(Field));
            }

            // The server requires a max_length type param on a VarChar element of an Array sub-field
            // (ErrorCode 1101); fail fast instead of letting the caller hit an opaque add-time error.
            if (Field.ElementDataType == DataType.VarChar && Field.MaxLength is null or <= 0)
            {
                throw new ArgumentException(
                    $"max_length is required for VarChar Array sub-field '{Field.Name}'.", nameof(Field));
            }
        }
        else if (Field.ElementNullable)
        {
            throw new ArgumentException(
                $"ElementNullable requires an Array sub-field, field '{Field.Name}'.", nameof(Field));
        }
    }
}
