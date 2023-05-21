using IO.Milvus.Client.REST;
using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Describe a collection
/// </summary>
internal class DescribeCollectionRequest:
    IRestRequest,
    IGrpcRequest<Grpc.DescribeCollectionRequest>,
    IValidatable
{
    /// <summary>
    /// Collection Name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The collection ID you want to describe
    /// </summary>
    [JsonPropertyName("collectionID")]
    public long CollectionId { get; set; }

    /// <summary>
    /// TimeStamp
    /// </summary>
    /// <remarks>
    /// If time_stamp is not zero, will return true when time_stamp >= created collection timestamp, otherwise will return false.
    /// </remarks>
    [JsonPropertyName("time_stamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Create a descriptioncollection request.
    /// </summary>
    /// <param name="collectionName">Milvus collection name.</param>
    public static DescribeCollectionRequest Create(string collectionName)
    {
        return new DescribeCollectionRequest(collectionName);
    }

    public DescribeCollectionRequest WithCollectionId(int collectionId)
    {
        CollectionId = collectionId;
        return this;
    }

    public DescribeCollectionRequest WithTimestamp(int timestamp)
    {
        Timestamp = timestamp;
        return this;
    }

    public HttpRequestMessage BuildRest()
    {
        this.Validate();

        return HttpRequest.CreateGetRequest(
            $"{ApiVersion.V1}/collection",
            payload: this
            );
    }

    public Grpc.DescribeCollectionRequest BuildGrpc()
    {
        this.Validate();

        return new Grpc.DescribeCollectionRequest()
        {
            CollectionID = CollectionId,
            CollectionName = CollectionName,
            TimeStamp = (ulong)Timestamp
        };
    }

    public void Validate()
    {
        Verify.ArgNotNullOrEmpty(CollectionName, "Milvus collection name cannot be null or empty");
    }

    #region private ================================================================================

    private DescribeCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }
    #endregion
}