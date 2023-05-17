using IO.Milvus.Param.Dml;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Insert rows of data entities into a collection
/// </summary>
internal class InsertRequest
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Db name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    /// <summary>
    /// Fields data
    /// </summary>
    [JsonPropertyName("fields_data")]
    public IList<Field> FieldsData { get; set; }

    /// <summary>
    /// Hash keys
    /// </summary>
    [JsonPropertyName("hash_keys")]
    public IList<int> HashKeys { get; set; }

    /// <summary>
    /// Number of rows
    /// </summary>
    [JsonPropertyName("num_rows")]
    public int NumRows { get; set; }

    /// <summary>
    /// Partition name
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }
}