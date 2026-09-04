namespace Milvus.Client.V2.Types;

/// <summary>
/// Defines the schema of a collection.
/// </summary>
public sealed class CollectionSchema
{
    private readonly List<FieldSchema> _fields = new();
    private readonly List<FunctionSchema> _functions = new();
    private readonly List<StructFieldSchema> _structFields = new();

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
    /// The functions defined in the schema (e.g. BM25 full-text search functions).
    /// </summary>
    public IList<FunctionSchema> Functions => _functions;

    /// <summary>
    /// The struct fields defined in the schema.
    /// </summary>
    public IList<StructFieldSchema> StructFields => _structFields;

    /// <summary>
    /// The properties of the collection, returned by describe.
    /// </summary>
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to enable dynamic fields for this schema. Defaults to <c>false</c>.
    /// </summary>
    public bool EnableDynamicFields { get; set; }
}
