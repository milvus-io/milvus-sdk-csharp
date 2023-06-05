using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetQuerySegmentInfoResponse
{
    [JsonPropertyName("infos")]
    public IList<MilvusQuerySegmentInfoResult> Infos { get; set; }

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }
}