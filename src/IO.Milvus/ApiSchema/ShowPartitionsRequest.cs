using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class ShowPartitionsRequest:
    IValidatable,
    IRestRequest,
    IGrpcRequest<Grpc.ShowPartitionsRequest>
{
    /// <summary>
    /// The collection name you want to describe, 
    /// you can pass collection_name or collectionID
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The collection id in milvus
    /// </summary>
    [JsonPropertyName("collectionID")]
    public int CollectionId { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [JsonPropertyName("db_name")]
    public string DbName { get; set; }

    /// <summary>
    /// Partition names
    /// </summary>
    /// <remarks>
    /// When type is InMemory, 
    /// will return these partitions inMemory_percentages.(Optional)
    /// </remarks>
    [JsonPropertyName("partition_names")]
    public IList<int> PartitionNames { get; set; }

    /// <summary>
    /// Decide return Loaded partitions or All partitions(Optional) 
    /// </summary>
    /// <remarks>
    /// Optional
    /// </remarks>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    internal static ShowPartitionsRequest Create(string collectionName,string dbName)
    {
        return new ShowPartitionsRequest(collectionName,dbName);
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty.");
        Verify.NotNullOrEmpty(DbName, "DbName cannot be null or empty");
    }

    public HttpRequestMessage BuildRest()
    {
        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/partitions",
            payload: this
            );
    }

    public Grpc.ShowPartitionsRequest BuildGrpc()
    {
        return new Grpc.ShowPartitionsRequest()
        {
            CollectionName = this.CollectionName,
            DbName = this.DbName
        };
    }

    #region Private =============================================================
    private ShowPartitionsRequest(string collectionName, string dbName)
    {
        this.CollectionName = collectionName;
        this.DbName = dbName;
    }
    #endregion
}
