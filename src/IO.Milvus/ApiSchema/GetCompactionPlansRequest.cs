using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get the plans of a compaction
/// </summary>
internal class GetCompactionPlansRequest
{
    /// <summary>
    /// Compaction ID
    /// </summary>
    [JsonPropertyName("compactionID")]
    public int CompactionID { get; set; }
}