using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Alter an alias
/// </summary>
internal class AlterAliasRequest
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
