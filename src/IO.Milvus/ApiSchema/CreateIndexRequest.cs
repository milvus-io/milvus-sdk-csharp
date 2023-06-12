using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
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

    [JsonPropertyName("index_name")]
    public string IndexName { get; set; }

    [JsonPropertyName("extra_params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> ExtraParams { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static CreateIndexRequest Create(
        string collectionName,
        string fieldName,
        MilvusIndexType milvusIndexType,
        MilvusMetricType milvusMetricType,
        string dbName)
    {
        return new CreateIndexRequest(collectionName, fieldName,milvusIndexType,milvusMetricType,dbName);
    }

    public CreateIndexRequest WithIndexName(string indexName)
    {
        if (!string.IsNullOrEmpty(indexName))
        {
            IndexName = indexName;
        }
        return this;
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
            DbName = this.DbName,
        };

        if (!string.IsNullOrEmpty(IndexName))
        {
            request.IndexName = IndexName;
        }

        request.ExtraParams.Add(new Grpc.KeyValuePair()
        {
            Key = "metric_type",
            Value = _milvusMetricType.ToString()
        });

        request.ExtraParams.Add(new Grpc.KeyValuePair()
        {
            Key = "index_type",
            Value = _milvusIndexType.ToString()
        });

        if (ExtraParams?.Any() == true)
        {
            request.ExtraParams.Add(new Grpc.KeyValuePair()
            {
                Key = "params",
                Value = ExtraParams.Combine()
            });
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        ExtraParams["metric_type"] = _milvusMetricType.ToString();
        ExtraParams["index_type"] = _milvusIndexType.ToString();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/index",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(FieldName, "Field name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(DbName, "DbName cannot be null or empty.");
    }

    #region Private ==================================================================================
    private MilvusMetricType _milvusMetricType;
    private MilvusIndexType _milvusIndexType;

    private CreateIndexRequest(
        string collectionName, 
        string fieldName,
        MilvusIndexType milvusIndexType,
        MilvusMetricType milvusMetricType,
        string dbName)
    {
        this.CollectionName = collectionName;
        this.FieldName = fieldName;
        this._milvusMetricType = milvusMetricType;
        this._milvusIndexType = milvusIndexType;
        this.DbName = dbName;
    }
    #endregion
}