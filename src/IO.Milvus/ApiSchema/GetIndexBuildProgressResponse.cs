using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class GetIndexBuildProgressResponse
{
    [JsonPropertyName("indexed_rows")]
    public long IndexedRows { get; set; }

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; }

    [JsonPropertyName("total_rows")]
    public long TotalRows { get; set; }

    public IndexBuildProgress ToIndexBuildProgress()
    {
        return new IndexBuildProgress(IndexedRows, TotalRows);
    }
}