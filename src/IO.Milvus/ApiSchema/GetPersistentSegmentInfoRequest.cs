using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Returns sealed segment information of a collection
/// </summary>
internal sealed class GetPersistentSegmentInfoRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.GetPersistentSegmentInfoRequest>
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    public static GetPersistentSegmentInfoRequest Create(string collectionName)
    {
        return new GetPersistentSegmentInfoRequest(collectionName);
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/persist/segment-info"
            );
    }

    public Grpc.GetPersistentSegmentInfoRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetPersistentSegmentInfoRequest()
        {
            CollectionName = this.CollectionName
        };
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
    }

    private GetPersistentSegmentInfoRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
}
