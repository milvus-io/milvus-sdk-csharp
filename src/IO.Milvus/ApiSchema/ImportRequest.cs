using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

/// <summary>
/// Import data files(json, numpy, etc.) on MinIO/S3 storage, read and parse them into sealed segments
/// </summary>
internal sealed class ImportRequest
{
    /// <summary>
    /// Channel names for the collection
    /// </summary>
    [JsonPropertyName("channel_names")]
    public IList<string> ChannelName { get; set; }

    /// <summary>
    /// Target collection name
    /// </summary>
    [JsonPropertyName("collection_name")]
    public string CollectionName { get; set; }

    /// <summary>
    /// File paths to be imported
    /// </summary>
    [JsonPropertyName("files")]
    public IList<string> Files { get; set;}

    /// <summary>
    /// Import options,bucket,etc
    /// </summary>
    [JsonPropertyName("options")]
    public IList<KeyValuePair<string,string>> Options { get; set;}

    /// <summary>
    /// Target partition
    /// </summary>
    [JsonPropertyName("partition_name")]
    public string PartitionName { get; set; }

    /// <summary>
    /// The file is row-based or column-based
    /// </summary>
    [JsonPropertyName("row_based")]
    public bool RowBased { get; set; }
}