using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// GetReplicas info of a collection
/// </summary>
internal sealed class GetReplicasRequest
{
    /// <summary>
    /// CollectionID
    /// </summary>
    [JsonPropertyName("collectionID")]
    public int CollectionID { get; set; }

    /// <summary>
    /// With shard nodes
    /// </summary>
    [JsonPropertyName("with_shard_nodes")]
    public bool WithShardNodes { get; set; }
}