using IO.Milvus.Client.REST;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Drop a collection
/// </summary>
internal sealed class DropCollectionRequest
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The unique collection name in milvus.(Required)
    /// </remarks>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    public static DropCollectionRequest Create(string collectionName)
    {
        return new DropCollectionRequest { CollectionName = collectionName };
    }

    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}collection",
            payload:this);
    }
}