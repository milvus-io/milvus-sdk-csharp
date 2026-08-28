using System.Diagnostics;

namespace Milvus.Client;
using System.Collections.Generic;

/// <summary>
/// The logical definition of a collection, describing the fields which make it up.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class CollectionSchema
{
    private readonly List<FieldSchema> _fields = new();
    private readonly List<FunctionSchema> _functions = new();

    /// <summary>
    /// Instantiates a new <see cref="CollectionSchema" />.
    /// </summary>
    public CollectionSchema()
    {
    }

    internal CollectionSchema(IReadOnlyList<FieldSchema> fields)
        : this(fields, Array.Empty<FunctionSchema>())
    {
    }

    internal CollectionSchema(IReadOnlyList<FieldSchema> fields, IReadOnlyList<FunctionSchema> functions)
    {
        _fields.AddRange(fields);
        _functions.AddRange(functions);
    }

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string? Name { get; set; } // TODO: does the schema really have a name separate from the collection's?

    /// <summary>
    /// An optional description for the collection.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The fields which make up the schema of the collection.
    /// </summary>
    public IList<FieldSchema> Fields => _fields;

    /// <summary>
    /// The functions defined in the schema for automatic data transformation.
    /// </summary>
    /// <remarks>
    /// Functions enable automatic data transformations during insertion, such as BM25 full-text
    /// search indexing where text fields are automatically converted to sparse vectors.
    /// </remarks>
    public IList<FunctionSchema> Functions => _functions;

    /// <summary>
    /// Whether to enable dynamic fields for this schema. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <see href="https://milvus.io/docs/dynamic_schema.md#JSON-a-new-data-type" />
    /// </remarks>
    public bool EnableDynamicFields { get; set; }

    // Note that an AutoId previously existed at the schema level, but is not deprecated.
    // AutoId is now only defined at the field level.

    private string DebuggerDisplay => $"{Name} ({Fields.Count} fields)";
}
