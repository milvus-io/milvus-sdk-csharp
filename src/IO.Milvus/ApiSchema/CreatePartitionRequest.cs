using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Create a partition.
/// </summary>
internal sealed class CreatePartitionRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.CreatePartitionRequest>
{
    /// <summary>
    /// Collection name.
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The partition name you want to create.
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }

    internal static CreatePartitionRequest Create(
        string collectionName, 
        string partitionName)
    {
        return new CreatePartitionRequest(collectionName, partitionName);
    }

    public Grpc.CreatePartitionRequest BuildGrpc()
    {
        return new Grpc.CreatePartitionRequest()
        {
            CollectionName = CollectionName,
            PartitionName = PartitionName,            
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/partition",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(PartitionName, "Milvus partition name cannot be null or empty.");
    }

    #region Private ====================================================================
    private CreatePartitionRequest(string collectionName, string partitionName)
    {
        CollectionName = collectionName;
        PartitionName = partitionName;
    }
    #endregion
}
