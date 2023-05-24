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
    /// Construct a field type.
    /// </summary>
    /// <param name="name">name</param>
    /// <param name="dataType"><see cref="MilvusDataType"/></param>
    /// <param name="isPrimaryKey"></param>
    /// <param name="autoId">Can not assign primary field data when auto id set as true.</param>
    public FieldType(
        string name,
        MilvusDataType dataType,
        bool isPrimaryKey,
        bool autoId = false)
    {
        Name = name;
        DataType = dataType;
        IsPrimaryKey = isPrimaryKey;
        AutoId = autoId;
    }

    /// <summary>
    /// Create a varchar.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="isPrimaryKey">Is primary key.</param>
    /// <param name="maxLength">Max length</param>
    /// <param name="autoId">Auto id</param>
    /// <returns></returns>
    public static FieldType CreateVarchar(
        string name,
        bool isPrimaryKey, 
        long maxLength,
        bool autoId = false)
    {
        var field = new FieldType(name, MilvusDataType.VarChar, isPrimaryKey, autoId);

        field.TypeParams.Add("max_length",maxLength.ToString());

        return field;
    }

    /// <summary>
    /// Create a float vector.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="dim">Dimension.</param>
    /// <returns></returns>
    public static FieldType CreateFloatVector(
        string name,
        long dim)
    {
        var field = new FieldType(name, MilvusDataType.FloatVector, false, false);

        field.TypeParams.Add("dim", dim.ToString());

        return field;
    }

    /// <summary>
    /// Auto id.
    /// </summary>
    [JsonPropertyName("autoID")]
    public bool AutoId { get; set; } = false;

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
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> IndexParams { get; } = new Dictionary<string,string>();

    /// <summary>
    /// Is primary key.
    /// </summary>
    [JsonPropertyName("is_primary_key")]
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    /// Type params.
    /// </summary>
    [JsonPropertyName("type_params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> TypeParams { get; } = new Dictionary<string,string>();
}
