namespace Milvus.Client.V2.Types;

/// <summary>
/// Defines the schema of a collection.
/// </summary>
public sealed class CollectionSchema
{
    private readonly List<FieldSchema> _fields = new();

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// An optional description for the collection.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The fields defined in the schema.
    /// </summary>
    public IList<FieldSchema> Fields => _fields;

    /// <summary>
    /// Whether to enable dynamic fields for this schema. Defaults to <c>false</c>.
    /// </summary>
    public bool EnableDynamicFields { get; set; }
}
