using IO.Milvus.Client.REST;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class CreateIndexRequest
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
        return new CreateIndexRequest(collectionName, fieldName, milvusIndexType, milvusMetricType, dbName);
    }

    public CreateIndexRequest WithIndexName(string indexName)
    {
        if (!string.IsNullOrEmpty(indexName))
        {
            IndexName = indexName;
        }
        return this;
    }

    public CreateIndexRequest WithExtraParams(IDictionary<string, string> extraParams)
    {
        if (extraParams == null)
            return this;

        foreach (KeyValuePair<string, string> param in extraParams)
        {
            ExtraParams[param.Key] = param.Value;
        }
        return this;
    }

    public Grpc.CreateIndexRequest BuildGrpc()
    {
        Grpc.CreateIndexRequest request = new()
        {
            CollectionName = CollectionName,
            FieldName = FieldName,
            DbName = DbName,
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

        if (ExtraParams?.Count > 0)
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
        ExtraParams["metric_type"] = _milvusMetricType.ToString();
        ExtraParams["index_type"] = _milvusIndexType.ToString();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/index",
            payload: this
            );
    }

    #region Private ==================================================================================
    private readonly MilvusMetricType _milvusMetricType;
    private readonly MilvusIndexType _milvusIndexType;

    private CreateIndexRequest(
        string collectionName,
        string fieldName,
        MilvusIndexType milvusIndexType,
        MilvusMetricType milvusMetricType,
        string dbName)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        _milvusMetricType = milvusMetricType;
        _milvusIndexType = milvusIndexType;
        DbName = dbName;
    }
    #endregion
}