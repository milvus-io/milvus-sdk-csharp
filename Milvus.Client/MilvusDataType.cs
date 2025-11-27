namespace Milvus.Client;

/// <summary>
/// Data type
/// </summary>
/// <remarks>
/// <see cref="Grpc.DataType"/>
/// </remarks>
public enum MilvusDataType
{
    /// <summary>
    /// None
    /// </summary>
    None = 0,

    /// <summary>
    /// Bool
    /// </summary>
    Bool = 1,

    /// <summary>
    /// Int8
    /// </summary>
    Int8 = 2,

    /// <summary>
    /// Int16
    /// </summary>
    Int16 = 3,

    /// <summary>
    /// Int32
    /// </summary>
    Int32 = 4,

    /// <summary>
    /// Int64
    /// </summary>
    Int64 = 5,

    /// <summary>
    /// Float
    /// </summary>
    Float = 10,

    /// <summary>
    /// Double
    /// </summary>
    Double = 11,

    /// <summary>
    /// String
    /// </summary>
    String = 20,

    /// <summary>
    /// VarChar
    /// </summary>
    VarChar = 21,

    /// <summary>
    /// Array
    /// </summary>
    Array = 22,

    /// <summary>
    /// Json
    /// </summary>
    Json = 23,

    /// <summary>
    /// BinaryVector
    /// </summary>
    BinaryVector = 100,

    /// <summary>
    /// FloatVector
    /// </summary>
    FloatVector = 101,

    /// <summary>
    /// Float16Vector
    /// </summary>
    Float16Vector = 102,

    /// <summary>
    /// BFloat16Vector
    /// </summary>
    BFloat16Vector = 103,

    /// <summary>
    /// SparseFloatVector. Available since Milvus v2.4.
    /// </summary>
    SparseFloatVector = 104,
}
