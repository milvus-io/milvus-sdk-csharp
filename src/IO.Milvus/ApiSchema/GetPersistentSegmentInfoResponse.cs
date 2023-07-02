using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetPersistentSegmentInfoResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("infos")]
    public IList<MilvusPersistentSegmentInfo> Infos { get; set; }
}