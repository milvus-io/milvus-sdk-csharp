using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Do a manual compaction
/// </summary>
internal sealed class ManualCompactionRequest
{
    /// <summary>
    /// Collection Id
    /// </summary>
    [JsonPropertyName("collectionID")]
    public long CollectionId { get; set; }

    /// <summary>
    /// Timetravel
    /// </summary>
    [JsonPropertyName("timetravel")]
    public long TimeTravel { get; set; }
}
