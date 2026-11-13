using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// Describes a struct field in a collection schema. A struct field groups a set of sub-fields (scalar or
/// vector); each row stores a list of struct values (one per struct element). Mirrors the Java/C++/Rust SDKs'
/// <c>StructFieldSchema</c>. Struct fields can be declared at collection creation time or added to an existing
/// collection via <c>AddCollectionStructField</c>.
/// </summary>
public sealed class StructFieldSchema
{
    private readonly List<FieldSchema> _fields = new();

    /// <summary>
    /// Initializes a new struct field schema.
    /// </summary>
    /// <param name="name">The struct field's name.</param>
    /// <param name="description">An optional description.</param>
    public StructFieldSchema(string name, string description = "")
    {
        Verify.NotNullOrWhiteSpace(name);
        Name = name;
        Description = description;
    }

    /// <summary>
    /// The struct field's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// An optional description of the struct field.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The maximum number of struct elements a single row may hold.
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Whether the struct field is nullable (row-level nullability: a whole struct row may be null). A struct
    /// field added to an existing collection (via <c>AddCollectionStructField</c>) must be nullable, mirroring
    /// the Java/C++/Rust SDKs. The nullable flag is propagated to every sub-field's wire <c>nullable</c> flag
    /// (the struct is nullable as a whole); a sub-field's own element-level nullability travels independently
    /// on the wire <c>element_nullable</c>, see <see cref="FieldSchema.Nullable" />.
    /// </summary>
    public bool Nullable { get; set; }

    /// <summary>
    /// Additional per-struct <c>type_params</c> to send on the wire, mirroring the Java SDK's
    /// <c>StructFieldSchema.typeParams</c> / pymilvus struct params. Supported keys include <c>warmup</c> and
    /// <c>mmap.enabled</c> (pymilvus accepts <c>mmap_enabled</c> and normalizes it to <c>mmap.enabled</c>; the C#
    /// SDK passes wire keys verbatim, like <see cref="FieldSchema.TypeParams" />).
    /// </summary>
    public IDictionary<string, string> TypeParams { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// The sub-fields of the struct. Sub-fields can be scalar, vector, or nested <see cref="DataType.Array" />
    /// types. A scalar/vector sub-field uses its own <see cref="FieldSchema.Nullable" /> to allow null element
    /// values; a nested Array sub-field additionally uses <see cref="FieldSchema.ElementNullable" /> to allow
    /// null values inside each inner array and requires <see cref="FieldSchema.MaxCapacity" /> for the inner
    /// array capacity. Sub-fields cannot be primary/partition/clustering keys, auto-id, or have a default
    /// value.
    /// </summary>
    public IList<FieldSchema> Fields => _fields;
}
