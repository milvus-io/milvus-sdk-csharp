using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create a collection
/// </summary>
internal sealed class CreateCollectionRequest
{
    #region Properties
    /// <summary>
    /// The unique collection name in milvus.(Required)
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The consistency level that the collection used, modification is not supported now.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item>"Strong": 0</item>
    /// <item>"Session": 1</item>
    /// <item>"Bounded": 2</item>
    /// <item>"Eventually": 3</item>
    /// <item>"Customized": 4</item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("consistency_level")]
    public MilvusConsistencyLevel ConsistencyLevel { get; set; }

    /// <summary>
    /// Once set, no modification is allowed (Optional)
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/milvus-io/milvus/issues/6690"/>
    /// </remarks>
    [JsonPropertyName("shards_num")]
    public int ShardsNum { get; set; } = 1;

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    /// <summary>
    /// Collection schema
    /// </summary>
    [JsonPropertyName("schema")]
    public CollectionSchema Schema { get; set; } = new CollectionSchema();
    #endregion

    public static void ValidateFieldTypes(IList<FieldType> fieldTypes)
    {
        Verify.NotNullOrEmpty(fieldTypes);
        FieldType firstField = fieldTypes[0];
        if (!firstField.IsPrimaryKey ||
            (firstField.DataType is not (MilvusDataType)DataType.Int64 and not (MilvusDataType)DataType.VarChar))
        {
            throw new MilvusException("The first FieldTypes's IsPrimaryKey must be true and DataType == Int64 or DataType == VarChar");
        }
        for (int i = 1; i < fieldTypes.Count; i++)
        {
            if (fieldTypes[i].IsPrimaryKey)
            {
                throw new ArgumentException("FieldTypes needs at most one primary key field type", "Schema.Fields");
            }
        }
    }
}