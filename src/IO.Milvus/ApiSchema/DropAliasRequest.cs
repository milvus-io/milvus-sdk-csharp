using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete an Alias
/// </summary>
internal class DropAliasRequest
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