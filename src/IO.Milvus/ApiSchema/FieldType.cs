using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

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

/// <summary>
/// Field type
/// </summary>
public sealed class FieldType
{
    /// <summary>
    /// Construct a fieldtype
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="dataType"></param>
    /// <param name="isPrimaryKey"></param>
    public FieldType(
        string name,
        MilvusDataType dataType,
        bool isPrimaryKey)
    {
        Name = name;
        DataType = dataType;
        IsPrimaryKey = isPrimaryKey;
    }

    /// <summary>
    /// Auto id.
    /// </summary>
    [JsonPropertyName("autoID")]
    public bool AutoId { get; set; } = true;

    /// <summary>
    /// Data type.
    /// </summary>
    [JsonPropertyName("data_type")]
    public MilvusDataType DataType { get; set; }

    /// <summary>
    /// Description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// Field id.
    /// </summary>
    [JsonPropertyName("fieldID")]
    public long FieldId { get; set; }

    /// <summary>
    /// Index params.
    /// </summary>
    [JsonPropertyName("index_params")]
    public Dictionary<string,string> IndexParams { get; set; }

    /// <summary>
    /// Is primary key.
    /// </summary>
    [JsonPropertyName("is_primary_key")]
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Type params.
    /// </summary>
    [JsonPropertyName("type_params")]
    public Dictionary<string,string> TypeParams { get; set; }
}
