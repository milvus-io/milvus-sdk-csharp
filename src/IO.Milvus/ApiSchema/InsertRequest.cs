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
    /// Partition name
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }

    /// <summary>
    /// Fields data
    /// </summary>
    [JsonPropertyName("fields_data")]
    public IList<Param.Dml.Field> FieldsData { get; set; }

    /// <summary>
    /// Number of rows
    /// </summary>
    [JsonPropertyName("num_rows")]
    public int NumRows { get; set; }
}