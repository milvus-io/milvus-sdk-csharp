using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Insert rows of data entities into a collection
/// </summary>
internal sealed class InsertRequest
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
    public IList<Field> FieldsData { get; set; }

    /// <summary>
    /// Number of rows
    /// </summary>
    [JsonPropertyName("num_rows")]
    public long NumRows { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static void ValidateFields(IList<Field> fields)
    {
        Verify.NotNullOrEmpty(fields);
        long count = fields[0].RowCount;
        for (int i = 1; i < fields.Count; i++)
        {
            if (fields[i].RowCount != count)
            {
                throw new ArgumentOutOfRangeException($"{nameof(fields)}[{i}])", "Fields length is not same");
            }
        }
    }
}