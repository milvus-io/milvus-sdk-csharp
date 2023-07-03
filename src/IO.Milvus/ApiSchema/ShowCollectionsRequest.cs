using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// ShowCollections
/// </summary>
internal sealed class ShowCollectionsRequest
{
    /// <summary>
    /// Collection Names
    /// </summary>
    /// <remarks>
    /// When type is InMemory, will return these collection's inMemory_percentages.(Optional)
    /// </remarks>
    [JsonPropertyName("collection_names")]
    public IList<string> CollectionNames { get; set; }

    /// <summary>
    /// Decide return Loaded collections or All collections(Optional)
    /// </summary>
    [JsonPropertyName("type")]
    public ShowType Type { get; set; } = ShowType.All;

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}