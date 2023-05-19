using IO.Milvus.Client.REST;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Drop a collection
/// </summary>
internal sealed class DropCollectionRequest:
    IRestRequest,
    IGrpcRequest<Grpc.DropCollectionRequest>
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
        return new DropCollectionRequest(collectionName);
    }

    public Grpc.DropCollectionRequest BuildGrpc()
    {
        return new Grpc.DropCollectionRequest
        {
            CollectionName = this.CollectionName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/collection",
            payload: this);
    }

    #region Private =================================================
    public DropCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}