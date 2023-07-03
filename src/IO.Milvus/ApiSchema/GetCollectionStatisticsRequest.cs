using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get a collection's statistics
/// </summary>
internal sealed class GetCollectionStatisticsRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The collection name you want get statistics
    /// </remarks>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}