using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class DescribeIndexRequest
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    [JsonIgnore]
    public string IndexName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static DescribeIndexRequest Create(string collectionName, string fieldName, string dbName)
    {
        return new DescribeIndexRequest(collectionName, fieldName, dbName);
    }

    public Grpc.DescribeIndexRequest BuildGrpc()
    {
        Validate();

        var request = new Grpc.DescribeIndexRequest()
        {
            CollectionName = CollectionName,
            FieldName = FieldName,
            DbName = DbName
        };

        if (!string.IsNullOrEmpty(IndexName))
        {
            request.IndexName = IndexName;
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/index",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FieldName);
        Verify.NotNullOrWhiteSpace(FieldName);
    }

    #region Private =========================================================================================
    private DescribeIndexRequest(string collectionName, string fieldName, string dbName)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        DbName = dbName;
    }
    #endregion
}
