using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class DropIndexRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.DropIndexRequest>
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    //[JsonPropertyName("index_name")]
    [JsonIgnore]
    public string IndexName { get; set; }

    public static DropIndexRequest Create(
        string collectionName,
        string fieldName)
    {
        return new DropIndexRequest(collectionName, fieldName);
    }

    public Grpc.DropIndexRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.DropIndexRequest()
        {
            CollectionName = this.CollectionName,
            FieldName = this.FieldName,
            IndexName = this.IndexName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/index",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(FieldName, "Field name cannot be null or empty.");
    }

    #region Private ======================================================
    public DropIndexRequest(string collectionName, string fieldName)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
    }
    #endregion
}