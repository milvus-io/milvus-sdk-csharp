using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Get the flush state of multiple segments
/// </summary>
internal sealed class GetFlushStateRequest
{
    /// <summary>
    /// Segment ids
    /// </summary>
    [JsonPropertyName("segmentIDs")]
    public IList<long> SegmentIds { get; set; }
}