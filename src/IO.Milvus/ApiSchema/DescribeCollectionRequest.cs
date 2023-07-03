using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Describe a collection
/// </summary>
internal sealed class DescribeCollectionRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The collection ID you want to describe
    /// </summary>
    [JsonPropertyName("collectionID")]
    public long CollectionId { get; set; }

    /// <summary>
    /// TimeStamp
    /// </summary>
    /// <remarks>
    /// If time_stamp is not zero, will return true when time_stamp >= created collection timestamp, otherwise will return false.
    /// </remarks>
    [JsonPropertyName("time_stamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }
}