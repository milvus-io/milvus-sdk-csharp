using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Collections.
/// </summary>
internal class ShowCollectionsResponse
{
    /// <summary>
    /// Collection Id list.
    /// </summary>
    [JsonPropertyName("collection_ids")]
    public IList<long> CollectionIds { get; set; }

    /// <summary>
    /// Collection name list.
    /// </summary>
    [JsonPropertyName("collection_names")]
    public IList<string> CollectionNames { get; set; }

    /// <summary>
    /// Hybrid timestamps in milvus.
    /// </summary>
    [JsonPropertyName("created_timestamps")]
    public IList<long> CreatedTimestamps { get; set; }

    /// <summary>
    /// The utc timestamp calculated by created_timestamp.
    /// </summary>
    [JsonPropertyName("created_utc_timestamps")]
    public IList<long> CreatedUtcTimestamps { get; set; }

    /// <summary>
    /// Load percentage on querynode when type is InMemory.
    /// </summary>
    [JsonPropertyName("inMemory_percentages")]
    public IList<int> InMemoryPercentages { get; set; }

    /// <summary>
    /// Status.
    /// </summary>
    public ResponseStatus Status { get; set; }

    public IEnumerable<MilvusCollection> ToCollections()
    {
        if (CollectionIds == null)
            yield break;

        for (int i = 0; i < CollectionIds.Count; i++)
        {
            yield return new MilvusCollection(
                CollectionIds[i],
                CollectionNames[i],
                TimestampUtils.GetTimeFromTimstamp(CreatedUtcTimestamps[i]),
                InMemoryPercentages?.Any() == true ? InMemoryPercentages[i] : -1);
        }
    }
}

/// <summary>
/// Milvus collection information
/// </summary>
public class MilvusCollection
{
    internal MilvusCollection(
        long id, 
        string name,
        DateTime createdUtcTime,
        long inMemoryPercentage)
    {
        Id = id;
        Name = name;
        CreatedUtcTime = createdUtcTime;
        InMemoryPercentage = inMemoryPercentage;
    }

    /// <summary>
    /// Collection Id list.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Collection name list.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The utc timestamp calculated by created_timestamp.
    /// </summary>
    public DateTime CreatedUtcTime { get; }

    /// <summary>
    /// Load percentage on querynode when type is InMemory.
    /// </summary>
    public long InMemoryPercentage { get; }
}