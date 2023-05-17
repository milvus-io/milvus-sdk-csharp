using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get the state of a compaction
/// </summary>
internal sealed class GetCompactionStateRequest
{
    /// <summary>
    /// Compaction ID
    /// </summary>
    [JsonPropertyName("compactionID")]
    public int CompactionID { get; set; }
}
