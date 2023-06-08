using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Load a collection for search
/// </summary>
internal sealed class LoadCollectionRequest :
    IRestRequest,
    IGrpcRequest<Grpc.LoadCollectionRequest>,
    IValidatable
{
    /// <summary>
    /// Collection Name
    /// </summary>
    /// <remarks>
    /// The collection name you want to load
    /// </remarks>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The replica number to load, default by 1
    /// </summary>
    [JsonPropertyName("replica_number")]
    public int ReplicaNumber { get; set; } = 1;

    public static LoadCollectionRequest Create(string collectionName)
    {
        return new LoadCollectionRequest(collectionName);
    }

    public LoadCollectionRequest WithReplicaNumber(int replicaNumber)
    {
        ReplicaNumber = replicaNumber;
        return this;
    }

    public Grpc.LoadCollectionRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.LoadCollectionRequest()
        {
            CollectionName = CollectionName,
            ReplicaNumber = ReplicaNumber,
        };
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/collection/load",
            this);
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.True(ReplicaNumber >= 1, "Replica number must be greater than 1.");
    }

    #region Private =====================================================================================
    private LoadCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}
