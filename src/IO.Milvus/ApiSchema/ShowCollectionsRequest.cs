using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// ShowCollections
/// </summary>
internal sealed class ShowCollectionsRequest
{
    /// <summary>
    /// Collection Names
    /// </summary>
    /// <remarks>
    /// When type is InMemory, will return these collection's inMemory_percentages.(Optional)
    /// </remarks>
    [JsonPropertyName("collection_names")]
    public IList<string> CollectionNames { get; set; }

    /// <summary>
    /// Decide return Loaded collections or All collections(Optional)
    /// </summary>
    [JsonPropertyName("type")]
    public ShowType Type { get; set; } = ShowType.All;

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static ShowCollectionsRequest Create(string dbName)
    {
        return new ShowCollectionsRequest(dbName);
    }

    public ShowCollectionsRequest WithCollectionNames(IList<string> collectionNames)
    {
        CollectionNames = collectionNames;
        return this;
    }

    public ShowCollectionsRequest WithType(ShowType type)
    {
        Type = type;
        return this;
    }

    public Grpc.ShowCollectionsRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.ShowCollectionsRequest()
        {
            Type = (Grpc.ShowType)this.Type,
            DbName = this.DbName
        };
        if (CollectionNames != null)
            request.CollectionNames.AddRange(CollectionNames);

        return request;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collections",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.NotNullOrWhiteSpace(DbName);
    }

    #region Private
    private ShowCollectionsRequest(string dbName)
    {
        this.DbName = dbName;
    }
    #endregion
}