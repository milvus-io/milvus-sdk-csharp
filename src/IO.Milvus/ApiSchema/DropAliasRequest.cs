using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete an Alias
/// </summary>
internal sealed class DropAliasRequest
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}