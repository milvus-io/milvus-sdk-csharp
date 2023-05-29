using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class LoadPartitionsRequest :
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.LoadPartitionsRequest>
{
    /// <summary>
    /// Collection name in milvus
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The partition names you want to load
    /// </summary>
    public IList<string> PartitionNames { get; set; }

    /// <summary>
    /// The replicas number you would load, 1 by default
    /// </summary>
    public int ReplicaNumber { get; set; } = 1;

    public Grpc.LoadPartitionsRequest BuildGrpc()
    {
        var request = new Grpc.LoadPartitionsRequest()
        {
            CollectionName = CollectionName,
            ReplicaNumber = ReplicaNumber,
        };
        request.PartitionNames.AddRange(PartitionNames);

        return request;
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.True(PartitionNames.Count >= 1, "Partition names count must be greater than 1");
        Verify.True(ReplicaNumber >= 1, "Replica number must be greater than 1.");
    }

    public static LoadPartitionsRequest Create(string collectionName)
    {
        return new LoadPartitionsRequest(collectionName);
    }

    public LoadPartitionsRequest WithPartitionNames(IList<string> partitionNames)
    {
        PartitionNames = partitionNames;
        return this;
    }

    public LoadPartitionsRequest WithReplicaNumber(int replicaNumber)
    {
        ReplicaNumber = replicaNumber;
        return this;
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/partitions/load",
            payload: this);
    }

    #region Private ==================================================
    public LoadPartitionsRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}