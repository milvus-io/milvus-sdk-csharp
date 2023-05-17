using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get the flush state of multiple segments
/// </summary>
internal class GetFlushStateRequest
{
    /// <summary>
    /// Segment ids
    /// </summary>
    [JsonPropertyName("segmentIDs")]
    public List<int> SegmentIDs { get; set; }
}

