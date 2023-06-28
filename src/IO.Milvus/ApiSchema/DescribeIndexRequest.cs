using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class DescribeIndexRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.DescribeIndexRequest>
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

    public static DescribeIndexRequest Create(string collectionName,string fieldName, string dbName)
    {
        return new DescribeIndexRequest(collectionName,fieldName, dbName);
    }

    public Grpc.DescribeIndexRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.DescribeIndexRequest()
        {
            CollectionName = this.CollectionName,
            FieldName = this.FieldName,
            DbName = this.DbName
        };

        if (!string.IsNullOrEmpty(this.IndexName))
        {
            request.IndexName = this.IndexName;
        }

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/index",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(FieldName, "Field name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(FieldName, "DbName cannot be null or empty.");
    }

    #region Private =========================================================================================
    private DescribeIndexRequest(string collectionName, string fieldName, string dbName)
    {
        this.CollectionName = collectionName;
        this.FieldName = fieldName;
        this.DbName = dbName;
    }
    #endregion
}
