using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Do a load balancing operation between query nodes
/// </summary>
internal sealed class LoadBalanceRequest
{
    /// <summary>
    /// Collection name
    /// </summary>
    [JsonPropertyName("collectionName")]
    public string CollectionName { get; set; }

    /// <summary>
    /// dst_nodeIDs 
    /// </summary>
    [JsonPropertyName("dst_nodeIDs")]
    public IList<int> DstNodeIds { get; set; }

    /// <summary>
    /// Sealed Segment ids
    /// </summary>
    [JsonPropertyName("sealed_segmentIDs")]
    public IList<int> SealedSegmentIds { get; set; }

    /// <summary>
    /// Source Node Id
    /// </summary>
    [JsonPropertyName("src_nodeID")]
    public int SrcNodeId { get; set; }
}