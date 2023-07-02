using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
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

    internal static GetQuerySegmentInfoRequest Create(string collectionName)
    {
        return new GetQuerySegmentInfoRequest(collectionName);
    }

    public Grpc.GetQuerySegmentInfoRequest BuildGrpc()
    {
        Validate();

        return new Grpc.GetQuerySegmentInfoRequest()
        {
            CollectionName = CollectionName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/query-segment-info",
            payload: this);
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
    }

    #region Private ==================================================================================
    private GetQuerySegmentInfoRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}