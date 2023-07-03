using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class SearchResponse
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("results")]
    public MilvusSearchResultData Results { get; set; }

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }
}