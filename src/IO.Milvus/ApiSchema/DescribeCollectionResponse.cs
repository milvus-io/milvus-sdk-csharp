using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Describe a collection.
/// </summary>
internal class DescribeCollectionResponse
{
    /// <summary>
    /// The aliases of this collection.
    /// </summary>
    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; set; }

    /// <summary>
    /// The collection name.
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// The collection id.
    /// </summary>
    [JsonPropertyName("collectionID")]
    public long CollectionId { get; set; }

    /// <summary>
    /// Consistency level.
    /// </summary>
    /// <remarks>
    /// The consistency level that the collection used, modification is not supported now.
    /// </remarks>
    [JsonPropertyName("consistency_level")]
    public MilvusConsistencyLevel ConsistencyLevel { get; set; }

    /// <summary>
    /// Hybrid timestamp in milvus.
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public long CreatedTimestamp { get; set; }

    /// <summary>
    /// The utc timestamp calculated by created_timestamp.
    /// </summary>
    [JsonPropertyName("created_utc_timestamp")]
    public long CreatedUTCTimestamp { get; set; }

    /// <summary>
    /// Collection Schema.
    /// </summary>
    [JsonPropertyName("schema")]
    public CollectionSchema Schema { get; set; }

    /// <summary>
    /// The shards number you set.
    /// </summary>
    [JsonPropertyName("shards_num")]
    public int ShardsNum { get; set; }

    /// <summary>
    /// The message ID/posititon when collection is created.
    /// </summary>
    [JsonPropertyName("start_positions")]
    public Dictionary<string, IList<int>> StartPostions { get; set; }

    /// <summary>
    /// Response status.
    /// </summary>
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    public DetailedMilvusCollection ToDetaildedMilvusCollection()
    {
        return new DetailedMilvusCollection(
            Aliases,
            CollectionName,
            CollectionId,
            ConsistencyLevel,
            TimestampUtils.GetTimeFromTimstamp(CreatedUTCTimestamp),
            Schema,
            ShardsNum,
            StartPostions
            );
    }
}

/// <summary>
/// Describle a milvus collection
/// </summary>
public class DetailedMilvusCollection
{
    internal DetailedMilvusCollection(
        IReadOnlyList<string> aliases, 
        string collectionName, 
        long collectionId, 
        MilvusConsistencyLevel consistencyLevel,
        DateTime createdUtcTime,
        CollectionSchema schema,
        int shardsNum,
        Dictionary<string,IList<int>> startPostions)
    {
        Aliases = aliases;
        CollectionName = collectionName;
        CollectionId = collectionId;
        ConsistencyLevel = consistencyLevel;
        CreatedUtcTime = createdUtcTime;
        Schema = schema;
        ShardsNum = shardsNum;
        StartPostions = startPostions;
    }

    /// <summary>
    /// The aliases of this collection.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// The collection name.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The collection id.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// Consistency level.
    /// </summary>
    /// <remarks>
    /// The consistency level that the collection used, modification is not supported now.
    /// </remarks>
    public MilvusConsistencyLevel ConsistencyLevel { get; }

    /// <summary>
    /// The utc timestamp calculated by created_timestamp.
    /// </summary>
    public DateTime CreatedUtcTime { get; }

    /// <summary>
    /// Collection Schema.
    /// </summary>
    public CollectionSchema Schema { get; }

    /// <summary>
    /// The shards number you set.
    /// </summary>
    public int ShardsNum { get; }

    /// <summary>
    /// The message ID/posititon when collection is created.
    /// </summary>
    public IDictionary<string, IList<int>> StartPostions { get; }
}