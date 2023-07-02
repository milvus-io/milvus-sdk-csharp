using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Field type
/// </summary>
public sealed class FieldType
{
    #region Ctor
    /// <summary>
    /// Construct a field type.
    /// </summary>
    /// <param name="name">name</param>
    /// <param name="dataType"><see cref="MilvusDataType"/></param>
    /// <param name="isPrimaryKey"></param>
    /// <param name="autoId">Can not assign primary field data when auto id set as true.</param>
    /// <param name="isPartitionKey">
    /// <c>Milvus v2.2.9</c>
    /// Sets the field to be partition key.</param>
    /// <param name="isDynamic">
    /// <c>Milvus v2.2.9</c>
    /// isDynamic of a field.</param>
    public FieldType(
        string name,
        MilvusDataType dataType,
        bool isPrimaryKey = false,
        bool autoId = false,
        bool isPartitionKey = false,
        bool isDynamic = false)
    {
        this.Name = name;
        this.DataType = dataType;
        this.IsPrimaryKey = isPrimaryKey;
        this.AutoId = autoId;
        this.IsPartitionKey = isPartitionKey;
        this.IsDynamic = isDynamic;
    }

    /// <summary>
    /// Create a field type.
    /// </summary>
    /// <param name="name">name</param>
    /// <param name="dataType"><see cref="MilvusDataType"/></param>
    /// <param name="isPrimaryKey"></param>
    /// <param name="autoId">Can not assign primary field data when auto id set as true.</param>
    /// <param name="isPartitionKey">
    /// <c>Milvus v2.2.9</c>
    /// Sets the field to be partition key.</param>
    /// <param name="isDynamic">
    /// <c>Milvus v2.2.9</c>
    /// isDynamic of a field.</param>
    public static FieldType Create(
        string name,
        MilvusDataType dataType,
        bool isPrimaryKey = false,
        bool autoId = false,
        bool isPartitionKey = false,
        bool isDynamic = false)
    {
        var field = new FieldType(name, dataType, isPrimaryKey, autoId, isPartitionKey, isDynamic);
        field.Validate();
        return field;
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
    /// <item><see cref="string"/> : string <see cref="MilvusDataType.VarChar"/>
    /// <see cref="FieldType.CreateVarchar(string, int, bool, bool, bool, bool)"/></item>
    /// </list>
    /// </typeparam>
    /// <param name="name">name</param>
    /// <param name="isPrimaryKey"></param>
    /// <param name="autoId">Can not assign primary field data when auto id set as true.</param>
    /// <param name="isPartitionKey">
    /// <c>Milvus v2.2.9</c>
    /// Sets the field to be partition key.</param>
    /// <param name="isDynamic">
    /// <c>Milvus v2.2.9</c>
    /// isDynamic of a field.</param>
    public static FieldType Create<TData>(
        string name,
        bool isPrimaryKey = false,
        bool autoId = false,
        bool isPartitionKey = false,
        bool isDynamic = false)
    {
        MilvusDataType dataType = Field.EnsureDataType<TData>();
        var field = new FieldType(name, dataType, isPrimaryKey, autoId, isPartitionKey, isDynamic);
        return field;
    }

    /// <summary>
    /// Create a varchar.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="isPrimaryKey">Is primary key.</param>
    /// <param name="maxLength">Max length</param>
    /// <param name="autoId">Auto id</param>
    /// <param name="isPartitionKey">
    /// <c>Milvus v2.2.9</c>
    /// Sets the field to be partition key.</param>
    /// <param name="isDynamic">
    /// <c>Milvus v2.2.9</c>
    /// isDynamic of a field.</param>
    public static FieldType CreateVarchar(
        string name,
        int maxLength,
        bool isPrimaryKey = false,
        bool autoId = false,
        bool isPartitionKey = false,
        bool isDynamic = false)
    {
        var field = new FieldType(name, MilvusDataType.VarChar, isPrimaryKey, autoId, isPartitionKey, isDynamic);
        field.WithMaxLength(maxLength);
        field.Validate();
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
        field.WithTypeParameter(Constants.VECTOR_DIM, dim.ToString(CultureInfo.InvariantCulture));
        field.Validate();
        return field;
    }

    /// <summary>
    /// Create a binary vector.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="dim">Dimension.</param>
    /// <returns></returns>
    public static FieldType CreateBinaryVector(
        string name,
        long dim)
    {
        var field = new FieldType(name, MilvusDataType.BinaryVector, false, false);
        field.WithTypeParameter(Constants.VECTOR_DIM, dim.ToString(CultureInfo.InvariantCulture));
        field.Validate();
        return field;
    }

    /// <summary>
    /// Create a json type field.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    /// <param name="name">Field name.</param>
    /// <param name="isDynamic">Is dynamic.</param>
    /// <returns></returns>
    public static FieldType CreateJson(string name, bool isDynamic = false)
    {
        return new FieldType(name, MilvusDataType.Json, false, isDynamic: isDynamic);
    }
    #endregion

    #region Properties
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

    /// <summary>
    /// Enable logic partitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A partition key field's values are hashed and distributed to different logic partitions.
    /// Only int64 and varchar type field can be partition key.
    /// Primary key field cannot be partition key.
    /// </para>
    /// <para>
    /// Not supported in restful api.
    /// </para>
    /// <c>For Milvus v2.2.9</c>
    /// </remarks>
    public bool IsPartitionKey { get; set; }

    /// <summary>
    /// Mark whether this field is the dynamic field.
    /// </summary>
    /// <remarks>
    /// <c>For Milvus v2.2.9</c>
    /// </remarks>
    public bool IsDynamic { get; set; }

    /// <summary>
    /// To keep compatible with older version, the default value is <see cref="MilvusFieldState.FieldCreated"/>.
    /// </summary>
    public MilvusFieldState FieldState { get; set; } = MilvusFieldState.FieldCreated;
    #endregion

    #region Methods
    /// <summary>
    /// Sets the max length of a <see cref="MilvusDataType.VarChar"/> field. The value must be greater than zero.
    /// </summary>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public FieldType WithMaxLength(int maxLength)
    {
        return WithTypeParameter(Constants.VARCHAR_MAX_LENGTH, maxLength.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///  Sets the dimension of a <see cref="MilvusDataType.FloatVector"/>. Dimension value must be greater than zero.
    /// </summary>
    /// <param name="dim"></param>
    /// <returns></returns>
    public FieldType WithDimension(int dim)
    {
        return WithTypeParameter(Constants.VECTOR_DIM, dim.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Sets a type parameter for the field.
    /// </summary>
    public FieldType WithTypeParameter(string key, string vlaue)
    {
        TypeParams[key] = vlaue;
        return this;
    }

    /// <summary>
    /// Sets a index parameter for the field.
    /// </summary>
    public FieldType WithIndexParameter(string key, string value)
    {
        IndexParams[key] = value;
        return this;
    }

    /// <summary>
    /// Validate
    /// </summary>
    public void Validate()
    {
        Verify.True(DataType != MilvusDataType.None, "Milvus field datatype cannot be None");
        Verify.True(DataType != MilvusDataType.String, "String type is not supported, use Varchar instead");

        if (DataType is MilvusDataType.FloatVector or MilvusDataType.BinaryVector)
        {
            Verify.True(TypeParams.ContainsKey(Constants.VECTOR_DIM), "Vector field dimension must be specified");

            if (int.TryParse(TypeParams[Constants.VECTOR_DIM], out int dim))
            {
                Verify.True(dim > 0, "Vector field dimension must be larger than zero");
            }
            else
            {
                throw new System.ArgumentException("Vector field dimension must be an integer number");
            }
        }
        else if (DataType == MilvusDataType.VarChar)
        {
            Verify.True(TypeParams.ContainsKey(Constants.VARCHAR_MAX_LENGTH), "Varchar field max length must be specified");

            if (int.TryParse(TypeParams[Constants.VARCHAR_MAX_LENGTH], out int maxLength))
            {
                Verify.True(maxLength > 0, "Varchar field max length must be larger than zero");
            }
            else
            {
                throw new System.ArgumentException("Varchar field max length must be an integer number");
            }
        }

        if (IsPrimaryKey || IsPartitionKey)
        {
            if (DataType is not MilvusDataType.Int64 and not MilvusDataType.VarChar)
            {
                throw new ArgumentException("Only Int64 and Varchar type field can be primary key or partition key");
            }
        }

        if (IsPartitionKey && IsPrimaryKey)
        {
            throw new ArgumentException("Primary key field can not be partition key");
        }
    }
    #endregion
}
