using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Returns growing segments's information of a collection
/// </summary>
internal sealed class GetQuerySegmentInfoRequest
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collectionName")]
    public string CollectionName { get; set; }

    [JsonPropertyName("dbName")]
    public string DbName { get; set; }
}