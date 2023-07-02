using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class ReleasePartitionRequest
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

    /// <summary>
    /// Database name
    /// </summary>
    /// <remarks>
    /// available in <c>Milvus 2.2.9</c>
    /// </remarks>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    public static ReleasePartitionRequest Create(string collectionName, string dbName)
    {
        return new ReleasePartitionRequest(collectionName, dbName);
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
            payload: this
            );
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.True(PartitionNames.Count >= 1, "Partition names count must be greater than 1");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    public Grpc.ReleasePartitionsRequest BuildGrpc()
    {
        this.Validate();

        var request = new Grpc.ReleasePartitionsRequest()
        {
            CollectionName = this.CollectionName,
            DbName = this.DbName
        };
        request.PartitionNames.AddRange(PartitionNames);

        return request;
    }

    #region Private ========================================================
    public ReleasePartitionRequest(string collectionName, string dbName)
    {
        this.CollectionName = collectionName;
        this.DbName = dbName;
    }
    #endregion
}