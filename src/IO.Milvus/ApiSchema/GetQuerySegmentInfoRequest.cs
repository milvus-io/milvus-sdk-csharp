using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Returns growing segments's information of a collection
/// </summary>
internal sealed class GetQuerySegmentInfoRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.GetQuerySegmentInfoRequest>
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collectionName")]
    public string CollectionName { get; set; }

    internal static GetQuerySegmentInfoRequest Create(string collectionName)
    {
        return new GetQuerySegmentInfoRequest(collectionName);
    }

    public Grpc.GetQuerySegmentInfoRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetQuerySegmentInfoRequest()
        {
            CollectionName = this.CollectionName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/query-segment-info",
            payload: this);
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
    }

    #region Private ==================================================================================
    private GetQuerySegmentInfoRequest(string collectionName)
    {
        this.CollectionName = collectionName;
    }
    #endregion
}