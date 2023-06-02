using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class QueryResponse
{
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("fields_data")]
    [JsonConverter(typeof(MilvusFieldConverter))]
    public IList<Field> FieldsData { get; set; }
}