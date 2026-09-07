namespace Milvus.Client.V2.Types;

/// <summary>
/// The data type of a field in a collection schema.
/// </summary>
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
    VarChar = 21,

    /// <summary>
    /// An array whose elements share a scalar data type.
    /// </summary>
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
    SparseFloatVector = 104,

    /// <summary>
    /// A vector whose elements are 8-bit integers.
    /// </summary>
    Int8Vector = 105,

    /// <summary>
    /// A structured data type that groups multiple fields.
    /// </summary>
    Struct = 201
}
