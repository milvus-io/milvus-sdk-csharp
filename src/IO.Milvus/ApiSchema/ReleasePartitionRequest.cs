using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class ReleasePartitionRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.ReleasePartitionsRequest>
{
    /// <summary>
    /// Collection name in milvus
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The partition names you want to load
    /// </summary>
    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; set; }

    public static ReleasePartitionRequest Create(string collectionName)
    {
        return new ReleasePartitionRequest(collectionName);
    }

    public ReleasePartitionRequest WithPartitionNames(IList<string> partitionNames)
    {
        PartitionNames = partitionNames;
        return this;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/partitions/load",
            payload:this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.True(PartitionNames.Count >= 1, "Partition names count must be greater than 1");
    }

    public Grpc.ReleasePartitionsRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.ReleasePartitionsRequest()
        {
            CollectionName = CollectionName,
        };
        request.PartitionNames.AddRange(PartitionNames);

        return request;
    }

    #region Private ========================================================
    public ReleasePartitionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}