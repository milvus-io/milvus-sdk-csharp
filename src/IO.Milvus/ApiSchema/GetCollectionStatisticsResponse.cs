using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetCollectionStatisticsResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("stats")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> Statistics { get; set; }
}