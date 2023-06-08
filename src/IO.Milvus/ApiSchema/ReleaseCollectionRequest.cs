using IO.Milvus.Client.REST;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Release a collection loaded before
/// </summary>
internal sealed class ReleaseCollectionRequest : 
    IRestRequest, 
    IGrpcRequest<Grpc.ReleaseCollectionRequest>
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The collection name you want to release
    /// </remarks>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    internal static ReleaseCollectionRequest Create(string collectionName)
    {
        return new ReleaseCollectionRequest(collectionName);
    }

    public Grpc.ReleaseCollectionRequest BuildGrpc()
    {
        return new Grpc.ReleaseCollectionRequest()
        {
            CollectionName = CollectionName,
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/collection/load",
            this
            );
    }

    #region Private ===============================================================
    private ReleaseCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}
