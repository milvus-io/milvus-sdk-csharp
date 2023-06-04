using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus;

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
        bool isPrimaryKey = false,
        bool autoId = false)
    {
        Name = name;
        DataType = dataType;
        IsPrimaryKey = isPrimaryKey;
        AutoId = autoId;
    }

    /// <summary>
    /// Create a field type.
    /// </summary>
    /// <param name="name">name</param>
    /// <param name="dataType"><see cref="MilvusDataType"/></param>
    /// <param name="isPrimaryKey"></param>
    /// <param name="autoId">Can not assign primary field data when auto id set as true.</param>
    public static FieldType Create(
        string name,
        MilvusDataType dataType,
        bool isPrimaryKey = false,
        bool autoId = false)
    {
        return new FieldType(name, dataType, isPrimaryKey, autoId);
    }

    /// <summary>
    /// Create a field type.
    /// </summary>
    /// <typeparam name="TData">
    /// Data type:If you use string , the data type will be <see cref="MilvusDataType.VarChar"/>
    /// <list type="bullet">
    /// <item><see cref="bool"/> : bool <see cref="MilvusDataType.Bool"/></item>
    /// <item><see cref="sbyte"/> : int8 <see cref="MilvusDataType.Int8"/></item>
    /// <item><see cref="Int16"/> : int16 <see cref="MilvusDataType.Int16"/></item>
    /// <item><see cref="int"/> : int32 <see cref="MilvusDataType.Int32"/></item>
    /// <item><see cref="long"/> : int64 <see cref="MilvusDataType.Int64"/></item>
    /// <item><see cref="float"/> : float <see cref="MilvusDataType.Float"/></item>
    /// <item><see cref="double"/> : double <see cref="MilvusDataType.Double"/></item>
    /// <item><see cref="string"/> : string <see cref="MilvusDataType.VarChar"/></item>
    /// </list>
    /// </typeparam>
    /// <param name="name">name</param>
    /// <param name="isPrimaryKey"></param>
    /// <param name="autoId">Can not assign primary field data when auto id set as true.</param>
    public static FieldType Create<TData>(
        string name,
        bool isPrimaryKey = false,
        bool autoId = false)
    {
        MilvusDataType dataType = Field.EnsureDataType<TData>();
        return new FieldType(name, dataType, isPrimaryKey, autoId);
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
        long maxLength,
        bool isPrimaryKey = false,
        bool autoId = false)
    {
        var field = new FieldType(name, MilvusDataType.VarChar, isPrimaryKey, autoId);

        field.TypeParams.Add("max_length", maxLength.ToString());

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
    public IDictionary<string, string> IndexParams { get; } = new Dictionary<string, string>();

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
    public IDictionary<string, string> TypeParams { get; } = new Dictionary<string, string>();
}
