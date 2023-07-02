using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetIndexBuildProgressRequest
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

    public static GetIndexBuildProgressRequest Create(string collectionName, string fieldName, string dbName)
    {
        return new GetIndexBuildProgressRequest(collectionName, fieldName, dbName);
    }

    public Grpc.GetIndexBuildProgressRequest BuildGrpc()
    {
        Validate();

        return new Grpc.GetIndexBuildProgressRequest()
        {
            CollectionName = CollectionName,
            FieldName = FieldName,
            DbName = DbName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/index/progress",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(FieldName);
        Verify.NotNullOrWhiteSpace(DbName);
    }

    #region Private ===================================================================================
    public GetIndexBuildProgressRequest(string collectionName, string fieldName, string dbName)
    {
        CollectionName = collectionName;
        FieldName = fieldName;
        DbName = dbName;
    }
    #endregion
}