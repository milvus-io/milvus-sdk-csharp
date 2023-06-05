using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetImportStateRequest
{
    [JsonPropertyName("task")]
    public int Task{ get; set; }
}