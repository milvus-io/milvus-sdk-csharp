using System;
using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Describe a collection.
/// </summary>
internal sealed class DescribeCollectionResponse
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
    /// The message ID/position when collection is created.
    /// </summary>
    [JsonPropertyName("start_positions")]
    public Dictionary<string, IList<int>> StartPositions { get; set; }

    /// <summary>
    /// Response status.
    /// </summary>
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    public DetailedMilvusCollection ToDetailedMilvusCollection()
    {
        return new DetailedMilvusCollection(
            (IReadOnlyList<string>)Aliases ?? Array.Empty<string>(),
            CollectionName,
            CollectionId,
            ConsistencyLevel,
            TimestampUtils.GetTimeFromTimstamp(CreatedUTCTimestamp),
            Schema,
            ShardsNum,
            StartPositions);
    }
}
