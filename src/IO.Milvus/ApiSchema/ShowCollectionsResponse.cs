using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Collections.
/// </summary>
public class ShowCollectionsResponse
{
    /// <summary>
    /// Collection Id list.
    /// </summary>
    [JsonPropertyName("collection_ids")]
    public IList<int> CollectionIds { get; set; }

    /// <summary>
    /// Collection name list.
    /// </summary>
    [JsonPropertyName("collection_names")]
    public IList<string> CollectionName { get; set; }

    /// <summary>
    /// Hybrid timestamps in milvus.
    /// </summary>
    [JsonPropertyName("created_timestamps")]
    public IList<int> CreatedTimestamps { get; set; }

    /// <summary>
    /// The utc timestamp calculated by created_timestamp.
    /// </summary>
    [JsonPropertyName("created_utc_timestamps")]
    public IList<int> CreatedUTCTimestamps { get; set; }

    /// <summary>
    /// Load percentage on querynode when type is InMemory.
    /// </summary>
    [JsonPropertyName("inMemory_percentages")]
    public IList<int> InMemoryPercentage { get; set; }

    /// <summary>
    /// Status.
    /// </summary>
    public ResponseStatus ResponseStatus { get; set; }
}