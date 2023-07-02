using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class DropIndexRequest
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    [JsonPropertyName("index_name")]
    public string IndexName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static DropIndexRequest Create(
        string collectionName,
        string fieldName,
        string indexName,
        string dbName)
    {
        return new DropIndexRequest(collectionName, fieldName, indexName, dbName);
    }

    public Grpc.DropIndexRequest BuildGrpc()
    {
        Validate();

        var request = new Grpc.DropIndexRequest()
        {
            CollectionName = CollectionName,
            FieldName = FieldName,
            IndexName = IndexName,
            DbName = DbName
        };

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/index",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FieldName);
        Verify.NotNullOrWhiteSpace(IndexName);
        Verify.NotNullOrWhiteSpace(DbName);
    }

    #region Private ======================================================
    public DropIndexRequest(string collectionName, string fieldName, string indexName, string dbName)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        IndexName = indexName;
        DbName = dbName;
    }
    #endregion
}