using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Do a mannual compaction
/// </summary>
internal sealed class ManualCompactionRequest
{
    /// <summary>
    /// Collection Id
    /// </summary>
    [JsonPropertyName("collectionID")]
    public int CollectionId { get; set; }

    /// <summary>
    /// Timetravel
    /// </summary>
    [JsonPropertyName("timetravel")]
    public int TimeTravel { get; set; }
}