using IO.Milvus.Client.REST;
using System;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Describe a collection
/// </summary>
public class DescriptionCollectionRequest
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
    public int? CollectionId { get; set; }

    /// <summary>
    /// TimeStamp
    /// </summary>
    /// <remarks>
    /// If time_stamp is not zero, will return true when time_stamp >= created collection timestamp, otherwise will return false.
    /// </remarks>
    [JsonPropertyName("time_stamp")]
    public int Timestamp { get; set; }

    /// <summary>
    /// Create a descriptioncollection request.
    /// </summary>
    /// <param name="collectionName">Milvus collection name.</param>
    public static DescriptionCollectionRequest Create(string collectionName)
    {
        return new DescriptionCollectionRequest(collectionName);
    }

    public DescriptionCollectionRequest WithCollectionId(int? collectionId)
    {
        CollectionId = collectionId;
        return this;
    }

    public DescriptionCollectionRequest WithTimestamp(int timestamp)
    {
        Timestamp = timestamp;
        return this;
    }

    public HttpRequestMessage Build()
    {
        throw new NotImplementedException();
        //return HttpRequest.CreateDeleteRequest()
    }

    #region private ================================================================================
    
    private DescriptionCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }

    #endregion
}