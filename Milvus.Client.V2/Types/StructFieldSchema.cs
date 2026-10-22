using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// Describes a struct field in a collection schema. A struct field groups a set of sub-fields (scalar or
/// vector); each row stores a list of struct values (one per struct element). Mirrors the Java/C++ SDKs'
/// <c>StructFieldSchema</c>. Struct fields are only defined at collection creation time; they cannot be added
/// to an existing collection.
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
    /// Additional per-struct <c>type_params</c> to send on the wire, mirroring the Java SDK's
    /// <c>StructFieldSchema.typeParams</c> / pymilvus struct params. Supported keys include <c>warmup</c> and
    /// <c>mmap.enabled</c> (pymilvus accepts <c>mmap_enabled</c> and normalizes it to <c>mmap.enabled</c>; the C#
    /// SDK passes wire keys verbatim, like <see cref="FieldSchema.TypeParams" />).
    /// </summary>
    public IDictionary<string, string> TypeParams { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// The sub-fields of the struct. Sub-fields cannot be primary/partition/clustering keys, cannot be
    /// auto-id, nullable, or have a default value.
    /// </summary>
    public IList<FieldSchema> Fields => _fields;
}
