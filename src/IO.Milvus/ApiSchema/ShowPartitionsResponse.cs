using IO.Milvus.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace IO.Milvus.ApiSchema;

internal sealed class ShowPartitionsResponse
{
    [JsonPropertyName("created_timestamps")]
    public IList<long> CreatedTimestamps { get; set; }

    [JsonPropertyName("created_utc_timestamps")]
    public IList<long> CreatedUtcTimestamps { get; set; }

    [JsonPropertyName("inMemory_percentages")]
    public IList<long> InMemoryPercentages { get; set; }

    [JsonPropertyName("partition_names")]
    public IList<string> PartitionNames { get; set; }

    [JsonPropertyName("partitionIDs")]
    public IList<long> PartitionIds { get; set; }

    [JsonPropertyName("")]
    public ResponseStatus Status { get; set; }

    public IEnumerable<MilvusPartition> ToMilvusPartitions()
    {
        if(PartitionNames?.Any() != true)
            yield break;

        for (int i = 0; i < PartitionNames.Count; i++)
        {
            yield return new MilvusPartition(
                PartitionIds[i],
                PartitionNames[i],
                TimestampUtils.GetTimeFromTimstamp(CreatedUtcTimestamps[i]),
                InMemoryPercentages?.Any() == true ? InMemoryPercentages[i] : -1
                );
        }
    }
}