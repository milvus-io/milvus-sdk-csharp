using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Release a collection loaded before
/// </summary>
internal sealed class ReleaseCollectionRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The collection name you want to release
    /// </remarks>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }
}
