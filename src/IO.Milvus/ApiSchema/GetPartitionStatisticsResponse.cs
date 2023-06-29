using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetPartitionStatisticsResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("stats")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string,string> Stats { get; set; }
}
