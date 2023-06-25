using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Flush a collection's data to disk. 
/// </summary>
/// <remarks>
/// Milvus data will be auto flushed.
/// Flush is only required when you want to get up to date entities numbers in statistics due to some internal mechanism. 
/// It will be removed in the future.
/// </remarks>
internal sealed class FlushRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.FlushRequest>
{   
    /// <summary>
    /// Collection names
    /// </summary>
    [JsonPropertyName("collection_names")]
    public IList<string> CollectionNames { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static FlushRequest Create(IList<string> collectionNames, string dbName)
    {
        return new FlushRequest(collectionNames, dbName);
    }

    public Grpc.FlushRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.FlushRequest();
        request.CollectionNames.AddRange(CollectionNames);

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/persist",
            payload: this);
    }

    public void Validate()
    {
        Verify.True(CollectionNames?.Any() == true, "Collection names list cannot be null or empty");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    #region Private ===========================================================================================
    private FlushRequest(IList<string> collectionNames, string dbName)
    {
        this.CollectionNames = collectionNames;
        this.DbName = dbName;
    }
    #endregion
}