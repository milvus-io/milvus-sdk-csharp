using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal class HasCollectionResponse
{
    /// <summary>
    /// Response status.
    /// </summary>
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("value")]
    public bool Value { get; set; }
}