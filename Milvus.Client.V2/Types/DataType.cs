namespace Milvus.Client.V2.Types;

/// <summary>
/// The data type of a field in a collection schema.
/// </summary>
// Member names mirror the Milvus data type names (Int8/Int16/Int32/Int64/Float/Double/String); renaming them
// to satisfy CA1720 would break the public API contract, so the rule is suppressed for this type.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Public enum member names mirror Milvus data type names.")]
public enum DataType
{
    /// <summary>
    /// No data type specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// A boolean data type.
    /// </summary>
    Bool = 1,

    /// <summary>
    /// An 8-bit signed integer.
    /// </summary>
    Int8 = 2,

    /// <summary>
    /// A 16-bit signed integer.
    /// </summary>
    Int16 = 3,

    /// <summary>
    /// A 32-bit signed integer.
    /// </summary>
    Int32 = 4,

    /// <summary>
    /// A 64-bit signed integer.
    /// </summary>
    Int64 = 5,

    /// <summary>
    /// A 32-bit floating-point number.
    /// </summary>
    Float = 10,

    /// <summary>
    /// A 64-bit floating-point number.
    /// </summary>
    Double = 11,

    /// <summary>
    /// A variable-length string data type, alias of <see cref="VarChar" />.
    /// </summary>
    String = 20,

    /// <summary>
    /// A variable-length string with a specified maximum length.
    /// </summary>
    /// <remarks>
    /// The maximum length is declared via <see cref="FieldSchema.MaxLength" /> (the <c>max_length</c> type
    /// param) and is required; values longer than it are rejected by the server.
    /// </remarks>
    VarChar = 21,

    /// <summary>
    /// An array whose elements share a scalar data type.
    /// </summary>
    /// <remarks>
    /// The element type is declared via <see cref="FieldSchema.ElementDataType" />, and the maximum number of
    /// elements per row via <see cref="FieldSchema.MaxCapacity" />. Arrays do not support default values.
    /// </remarks>
    Array = 22,

    /// <summary>
    /// A JSON data type.
    /// </summary>
    Json = 23,

    /// <summary>
    /// A geometry data type for spatial data, expressed in GeoJSON.
    /// </summary>
    Geometry = 24,

    /// <summary>
    /// A timezone-aware timestamp data type.
    /// </summary>
    Timestamptz = 26,

    /// <summary>
    /// A binary vector whose elements are single bits.
    /// </summary>
    BinaryVector = 100,

    /// <summary>
    /// A float vector whose elements are 32-bit floats.
    /// </summary>
    FloatVector = 101,

    /// <summary>
    /// A float vector whose elements are 16-bit floats.
    /// </summary>
    Float16Vector = 102,

    /// <summary>
    /// A float vector whose elements are bfloat16 values.
    /// </summary>
    BFloat16Vector = 103,

    /// <summary>
    /// A sparse float vector that stores only non-zero elements.
    /// </summary>
    /// <remarks>
    /// Rows are expressed as a <see cref="MilvusSparseVector{T}" /> (indices + values); indices are
    /// zero-based and values must be finite. Suited to BM25/full-text and high-dimensional one-hot data.
    /// </remarks>
    SparseFloatVector = 104,

    /// <summary>
    /// A vector whose elements are 8-bit integers.
    /// </summary>
    Int8Vector = 105,

    /// <summary>
    /// A structured data type that groups multiple fields.
    /// </summary>
    /// <remarks>
    /// Struct fields group a set of sub-fields (scalar or vector) and can only be defined at collection
    /// creation time via <see cref="CollectionSchema.StructFields" />; each row stores a list of struct
    /// values. See <see cref="StructFieldSchema" /> and <see cref="StructFieldData" />.
    /// </remarks>
    Struct = 201
}
