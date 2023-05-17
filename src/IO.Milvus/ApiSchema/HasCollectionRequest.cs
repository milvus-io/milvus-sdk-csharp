using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get if a collection's existence
/// </summary>
internal sealed class HasCollectionRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The unique collection name in milvus.(Required)
    /// </remarks>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// TimeStamp
    /// </summary>
    /// <remarks>
    /// If time_stamp is not zero, will return true when time_stamp >= created collection timestamp, otherwise will return false.
    /// </remarks>
    [JsonPropertyName("time_stamp")]
    public int TimeStamp { get; set; }
}
