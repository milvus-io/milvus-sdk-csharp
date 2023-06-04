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

    [JsonPropertyName("index_name")]
    public string IndexName { get; set; }

    public static DropIndexRequest Create(
        string collectionName,
        string fieldName,
        string indexName)
    {
        return new DropIndexRequest(collectionName, fieldName, indexName);
    }

    public Grpc.DropIndexRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.DropIndexRequest()
        {
            CollectionName = this.CollectionName,
            FieldName = this.FieldName,
            IndexName = this.IndexName
        };

        return request;
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
        Verify.ArgNotNullOrEmpty(IndexName, $"Index name cannot be null or empty.The default index name is Constants.DEFAULT_INDEX_NAME :{Constants.DEFAULT_INDEX_NAME}");
    }

    #region Private ======================================================
    public DropIndexRequest(string collectionName, string fieldName, string indexName)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        IndexName = indexName;
    }
    #endregion
}