using System.Diagnostics;

namespace Milvus.Client;

/// <summary>
/// Defines a function within a <see cref="CollectionSchema" /> that automatically transforms data during insertion.
/// </summary>
/// <remarks>
/// Functions enable automatic data transformations such as BM25 full-text search indexing,
/// where text fields are automatically converted to sparse vectors.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionSchema
{
    private readonly List<string> _inputFieldNames = new();
    private readonly List<string> _outputFieldNames = new();
    private readonly Dictionary<string, string> _params = new();

    /// <summary>
    /// Creates a new function schema.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <param name="type">The type of function.</param>
    /// <param name="inputFieldNames">The names of the input fields for this function.</param>
    /// <param name="outputFieldNames">The names of the output fields for this function.</param>
    /// <param name="description">An optional description for the function.</param>
    public FunctionSchema(
        string name,
        FunctionType type,
        IEnumerable<string> inputFieldNames,
        IEnumerable<string> outputFieldNames,
        string description = "")
    {
        Name = name;
        Type = type;
        _inputFieldNames.AddRange(inputFieldNames);
        _outputFieldNames.AddRange(outputFieldNames);
        Description = description;
    }

    /// <summary>
    /// Creates a BM25 function schema for full-text search.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <param name="inputFieldName">
    /// The name of the VARCHAR input field. This field must have <see cref="FieldSchema.EnableAnalyzer"/> set to true.
    /// </param>
    /// <param name="outputFieldName">
    /// The name of the SPARSE_FLOAT_VECTOR output field that will be automatically populated.
    /// </param>
    /// <param name="description">An optional description for the function.</param>
    /// <returns>A new BM25 function schema.</returns>
    public static FunctionSchema CreateBm25(
        string name,
        string inputFieldName,
        string outputFieldName,
        string description = "")
        => new(name, FunctionType.Bm25, new[] { inputFieldName }, new[] { outputFieldName }, description);

    internal FunctionSchema(
        long id,
        string name,
        FunctionType type,
        IEnumerable<string> inputFieldNames,
        IEnumerable<string> outputFieldNames,
        string description,
        IEnumerable<KeyValuePair<string, string>> parameters)
    {
        Id = id;
        Name = name;
        Type = type;
        _inputFieldNames.AddRange(inputFieldNames);
        _outputFieldNames.AddRange(outputFieldNames);
        Description = description;
        foreach (var param in parameters)
        {
            _params[param.Key] = param.Value;
        }
    }

    /// <summary>
    /// The internal Milvus ID assigned to this function.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// The name of the function.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of function.
    /// </summary>
    public FunctionType Type { get; }

    /// <summary>
    /// The names of the input fields for this function.
    /// </summary>
    public IList<string> InputFieldNames => _inputFieldNames;

    /// <summary>
    /// The names of the output fields for this function.
    /// </summary>
    public IList<string> OutputFieldNames => _outputFieldNames;

    /// <summary>
    /// An optional description for the function.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Additional parameters for the function.
    /// </summary>
    public IDictionary<string, string> Params => _params;

    private string DebuggerDisplay => $"{Name} ({Type})";
}
