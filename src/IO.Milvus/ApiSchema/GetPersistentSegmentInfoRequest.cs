using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Returns sealed segment information of a collection
/// </summary>
internal sealed class GetPersistentSegmentInfoRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.GetPersistentSegmentInfoRequest>
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static GetPersistentSegmentInfoRequest Create(string collectionName,string dbName)
    {
        return new GetPersistentSegmentInfoRequest(collectionName, dbName);
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/persist/segment-info"
            );
    }

    public Grpc.GetPersistentSegmentInfoRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.GetPersistentSegmentInfoRequest()
        {
            CollectionName = this.CollectionName,
            DbName = this.DbName
        };
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    private GetPersistentSegmentInfoRequest(string collectionName,string dbName)
    {
        this.CollectionName = collectionName;
        this.DbName = dbName;
    }
}
