using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class FlushResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("coll_segIDs")]
    public IDictionary<string,MilvusId<long>> CollSegIDs { get; set; }

    [JsonPropertyName("flush_coll_segIDs")]
    public IDictionary<string, MilvusId<long>> FlushCollSegIds { get; set; }

    [JsonPropertyName("coll_seal_times")]
    public IDictionary<string,long> CollSealTimes { get; set; }
}