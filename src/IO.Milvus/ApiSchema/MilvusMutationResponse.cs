using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class MilvusMutationResponse
{
    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("IDs")]
    public MilvusIds Ids { get; set; }

    [JsonPropertyName("insert_cnt")]
    public long InsertCount { get; set; }

    [JsonPropertyName("delete_cnt")]
    public long DeletedCount { get; set; }

    [JsonPropertyName("upsert_cnt")]
    public long UpsertCount { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("acknowledged")]
    public bool Acknowledged { get; set; }

    [JsonPropertyName("succ_index")]
    public IList<uint> SuccessIndex { get; set; }

    [JsonPropertyName("err_index")]
    public IList<uint> ErrorIndex { get; set; }
}