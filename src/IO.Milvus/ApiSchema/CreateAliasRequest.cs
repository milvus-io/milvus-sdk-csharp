using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create an alias for a collection name
/// </summary>
internal class CreateAliasRequest
{
    public string Alias { get; set; }

    //TODO:base

    /// <summary>
    /// Collection Name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}
