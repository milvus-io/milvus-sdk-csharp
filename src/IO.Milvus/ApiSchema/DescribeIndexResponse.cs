using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class DescribeIndexResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("index_descriptions")]
    public IList<IndexDescription> IndexDescriptions { get; set; }

    public IEnumerable<MilvusIndex> ToMilvusIndexes()
    {
        if (IndexDescriptions?.Any() != true)
        {
            yield break;
        }

        foreach (IndexDescription index in IndexDescriptions)
        {
            yield return new MilvusIndex(index.FieldName, index.IndexName, index.IndexId, index.Params);
        }
    }
}

internal sealed class IndexDescription
{
    [JsonPropertyName("field_name")]
    public string FieldName { get; set; }

    [JsonPropertyName("index_name")]
    public string IndexName { get; set; }

    [JsonPropertyName("index_id")]
    public long IndexId { get; set; }

    [JsonPropertyName("params")]
    [JsonConverter(typeof(MilvusDictionaryConverter))]
    public IDictionary<string, string> Params { get; set; }
}
