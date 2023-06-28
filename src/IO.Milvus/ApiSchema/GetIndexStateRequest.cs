using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetIndexStateRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.GetIndexStateRequest>
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static GetIndexStateRequest Create(string collectionName, string fieldName, string dbName)
    {
        return new GetIndexStateRequest(collectionName, fieldName, dbName);
    }

    public Grpc.GetIndexStateRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetIndexStateRequest()
        {
            CollectionName = this.CollectionName,
            FieldName = this.FieldName,
            DbName = this.DbName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/state",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(FieldName, "Field name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(DbName, "DbName cannot be null or empty.");
    }

    #region Private ====================================================================================
    public GetIndexStateRequest(string collectionName, string fieldName, string dbName)
    {
        this.CollectionName = collectionName;
        this.FieldName = fieldName;
        this.DbName = dbName;
    }
    #endregion
}