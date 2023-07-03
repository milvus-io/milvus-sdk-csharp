using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class CalcDistanceRequest
{
    /// <summary>
    /// Left vectors to calculate distance.
    /// </summary>
    [JsonPropertyName("op_left")]
    public MilvusVectors VectorsLeft { get; set; }

    /// <summary>
    /// Right vectors to calculate distance.
    /// </summary>
    [JsonPropertyName("op_right")]
    public MilvusVectors VectorsRight { get; set; }

    [JsonPropertyName("params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> Params { get; set; } = new Dictionary<string, string>();
}