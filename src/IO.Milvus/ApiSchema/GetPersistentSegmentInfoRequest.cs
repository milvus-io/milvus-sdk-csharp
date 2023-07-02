using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Returns sealed segment information of a collection
/// </summary>
internal sealed class GetPersistentSegmentInfoRequest
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

    public static GetPersistentSegmentInfoRequest Create(string collectionName, string dbName)
    {
        return new GetPersistentSegmentInfoRequest(collectionName, dbName);
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/persist/segment-info"
            );
    }

    public Grpc.GetPersistentSegmentInfoRequest BuildGrpc()
    {
        Validate();

        return new Grpc.GetPersistentSegmentInfoRequest()
        {
            CollectionName = CollectionName,
            DbName = DbName
        };
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(DbName);
    }

    private GetPersistentSegmentInfoRequest(string collectionName, string dbName)
    {
        CollectionName = collectionName;
        DbName = dbName;
    }
}
