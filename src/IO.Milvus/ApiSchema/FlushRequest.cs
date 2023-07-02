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
internal sealed class FlushRequest
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
        Validate();

        var request = new Grpc.FlushRequest();
        request.CollectionNames.AddRange(CollectionNames);

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        Validate();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/persist",
            payload: this);
    }

    public void Validate()
    {
        Verify.NotNullOrEmpty(CollectionNames);
        Verify.NotNullOrWhiteSpace(DbName);
    }

    #region Private ===========================================================================================
    private FlushRequest(IList<string> collectionNames, string dbName)
    {
        CollectionNames = collectionNames;
        DbName = dbName;
    }
    #endregion
}