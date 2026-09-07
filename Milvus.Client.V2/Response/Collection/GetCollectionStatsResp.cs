using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Collection;

/// <summary>
/// Represents the result of a <c>GetCollectionStats</c> operation.
/// </summary>
public sealed class GetCollectionStatsResp
{
    private GetCollectionStatsResp(long rowCount)
    {
        RowCount = rowCount;
    }

    internal static GetCollectionStatsResp FromGrpc(Grpc.GetCollectionStatisticsResponse response)
    {
        long rowCount = 0;
        foreach (Grpc.KeyValuePair stat in response.Stats)
        {
            if (stat.Key == "row_count")
            {
                rowCount = long.Parse(stat.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return new GetCollectionStatsResp(rowCount);
    }

    /// <summary>
    /// The number of rows in the collection.
    /// </summary>
    public long RowCount { get; }
}
