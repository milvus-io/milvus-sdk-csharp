namespace IO.Milvus;

/// <summary>
/// Data type
/// </summary>
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
    /// BinaryVector
    /// </summary>
    BinaryVector = 100,

    /// <summary>
    /// FloatVector
    /// </summary>
    FloatVector = 101,
}
