using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Delete a partition
/// </summary>
internal sealed class DropPartitionRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.DropPartitionRequest>
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// Partition name
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }

    internal static DropPartitionRequest Create(string collectionName, string partitionName)
    {
        return new DropPartitionRequest(collectionName, partitionName);
    }

    public Grpc.DropPartitionRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.DropPartitionRequest()
        {
            CollectionName = CollectionName,
            PartitionName = PartitionName
        };
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/partition",
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.ArgNotNullOrEmpty(PartitionName, "Milvus partition name cannot be null or empty.");
    }

    #region Private =========================================================================
    public DropPartitionRequest(string collectionName, string partitionName)
    {
        CollectionName = collectionName;
        PartitionName = partitionName;
    }
    #endregion
}
