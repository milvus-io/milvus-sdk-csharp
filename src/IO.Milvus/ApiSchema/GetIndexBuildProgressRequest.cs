using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetIndexBuildProgressRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.GetIndexBuildProgressRequest>
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    public static GetIndexBuildProgressRequest Create(string collectionName, string fieldName)
    {
        return new GetIndexBuildProgressRequest(collectionName, fieldName);
    }

    public Grpc.GetIndexBuildProgressRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetIndexBuildProgressRequest()
        {
            CollectionName = this.CollectionName,
            FieldName = this.FieldName,
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/index/progress",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(FieldName, "Field name cannot be null or empty.");
    }

    #region Private ===================================================================================
    public GetIndexBuildProgressRequest(string collectionName, string fieldName)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
    }
    #endregion
}