using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class CalDistanceResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("Array")]
    public MilvusDistanceResponse MilvusDistance { get; set; }
}

internal sealed class MilvusDistanceResponse {
    /// <summary>
    /// Int distance value.
    /// </summary>
    [JsonPropertyName("IntDist")]
    public DataList<int> IntDistance { get; set; }

    /// <summary>
    /// Float distance value.
    /// </summary>
    [JsonPropertyName("FloatDist")]
    public DataList<float> FloatDistance { get; set; }
}

internal sealed class DataList<TData>
{
    [JsonPropertyName("data")]
    public IList<TData> Data { get; set; }
}