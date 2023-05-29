using IO.Milvus.Client.REST;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Show type
/// </summary>
public enum ShowType
{
    /// <summary>
    /// Will return all collections
    /// </summary>
    All = 0,
    
    /// <summary>
    /// Will return loaded collections with their inMemory_percentages
    /// </summary>
    InMemory = 1,
}

/// <summary>
/// ShowCollections
/// </summary>
internal sealed class ShowCollectionsRequest:
    IRestRequest,
    IGrpcRequest<Grpc.ShowCollectionsRequest>,
    IValidatable
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

    public static ShowCollectionsRequest Create()
    {
        return new ShowCollectionsRequest();
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
            Type = (Grpc.ShowType)Type
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
            payload:this
            );
    }

    public void Validate() { }
}