using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Flush a collection's data to disk. 
/// </summary>
/// <remarks>
/// Milvus's data will be auto flushed.
/// Flush is only required when you want to get up to date entities numbers in statistics due to some internal mechanism. 
/// It will be removed in the future.
/// </remarks>
internal class FlushRequest
{
    /// <summary>
    /// Collection names
    /// </summary>
    [JsonPropertyName("collection_names")]
    public IList<string> CollectionNames { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}