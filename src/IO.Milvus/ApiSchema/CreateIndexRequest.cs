using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class CreateIndexRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.CreateIndexRequest>
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    [JsonPropertyName("extra_params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> ExtraParams { get; set; }

    public static CreateIndexRequest Create(
        string collectionName,
        string fieldName,
        MilvusIndexType milvusIndexType,
        MilvusMetricType milvusMetricType)
    {
        return new CreateIndexRequest(collectionName, fieldName,milvusIndexType,milvusMetricType);
    }

    public CreateIndexRequest WithExtraParams(IDictionary<string,string> extraParams)
    {
        ExtraParams = extraParams;
        return this;
    }

    public Grpc.CreateIndexRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.CreateIndexRequest()
        {
            CollectionName = this.CollectionName,
            FieldName = this.FieldName,
        };

        if (ExtraParams?.Any() == true)
        {
            request.ExtraParams.AddRange(ExtraParams.Select(p =>
                new Grpc.KeyValuePair()
                {
                    Key = p.Key,
                    Value = p.Value
                }));
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/index",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(FieldName, "Field name cannot be null or empty.");
    }

    #region Private ====================================================================
    private CreateIndexRequest(
        string collectionName, 
        string fieldName, 
        MilvusIndexType milvusIndexType, 
        MilvusMetricType milvusMetricType)
    {
        this.CollectionName = collectionName;
        this.FieldName = fieldName;
        
    }
    #endregion
}