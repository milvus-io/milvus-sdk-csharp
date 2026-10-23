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

    /// <summary>
    /// The external data source of an external collection, e.g. <c>"s3://bucket/path"</c>. Set together with
    /// <see cref="ExternalSpec" /> and the external-field mappings on <see cref="FieldSchema" /> to create an
    /// external collection. Mirrors the Java/C++/Rust SDKs' <c>externalSource</c>.
    /// </summary>
    public string? ExternalSource { get; set; }

    /// <summary>
    /// The external file specification of an external collection (a JSON string), e.g. <c>"{\"format\":\"parquet\"}"</c>.
    /// Mirrors the Java/C++/Rust SDKs' <c>externalSpec</c>.
    /// </summary>
    public string? ExternalSpec { get; set; }
}
