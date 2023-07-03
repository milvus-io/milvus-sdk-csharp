using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class DropIndexRequest
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    [JsonPropertyName("index_name")]
    public string IndexName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}