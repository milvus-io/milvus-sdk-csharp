#pragma warning disable CS1591 // Missing XML docs

namespace Milvus.Client.V2.Responses.Partition;
public sealed class GetPartitionStatsResp
{
    private GetPartitionStatsResp(long rowCount) => RowCount = rowCount;
    internal static GetPartitionStatsResp FromGrpc(Grpc.GetPartitionStatisticsResponse response)
    {
        long rowCount = 0;
        foreach (Grpc.KeyValuePair stat in response.Stats)
        {
            if (stat.Key == "row_count")
            {
                rowCount = long.Parse(stat.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        return new GetPartitionStatsResp(rowCount);
    }
    public long RowCount { get; }
}
