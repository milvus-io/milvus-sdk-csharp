using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Security;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get a collection's statistics
/// </summary>
internal sealed class GetCollectionStatisticsRequest:
    IRestRequest,
    IGrpcRequest<Grpc.GetCollectionStatisticsRequest>,
    IValidatable
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The collection name you want get statistics
    /// </remarks>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    public static GetCollectionStatisticsRequest Create(string collectionName)
    {
        return new GetCollectionStatisticsRequest(collectionName);
    }

    public Grpc.GetCollectionStatisticsRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetCollectionStatisticsRequest()
        {
            CollectionName = CollectionName,
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collection/statistics",
            payload:this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
    }

    #region Private ==================================================
    public GetCollectionStatisticsRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}