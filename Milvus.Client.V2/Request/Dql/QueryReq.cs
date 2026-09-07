using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// Represents a request to query rows from a collection by expression.
/// </summary>
public sealed class QueryReq
{
    /// <summary>
    /// The name of the collection to query.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The boolean expression identifying the rows to return (e.g. <c>"id in [1, 2, 3]"</c>).
    /// </summary>
    public string Expression { get; set; } = "";

    /// <summary>
    /// The optional query parameters.
    /// </summary>
    public QueryParameters? Parameters { get; set; }

    internal Grpc.QueryRequest ToGrpcQueryRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrWhiteSpace(Expression);

        var request = new Grpc.QueryRequest
        {
            CollectionName = CollectionName,
            Expr = Expression
        };

        if (Parameters is not null)
        {
            if (Parameters.PartitionNamesInternal?.Count > 0)
            {
                request.PartitionNames.AddRange(Parameters.PartitionNamesInternal);
            }
            if (Parameters.OutputFieldsInternal?.Count > 0)
            {
                request.OutputFields.AddRange(Parameters.OutputFieldsInternal);
            }
            if (Parameters.Limit is not null)
            {
                request.QueryParams.Add(new Grpc.KeyValuePair { Key = "limit", Value = Parameters.Limit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (Parameters.Offset is not null)
            {
                request.QueryParams.Add(new Grpc.KeyValuePair { Key = "offset", Value = Parameters.Offset.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            }
            if (Parameters.TimeTravelTimestamp is not null)
            {
                request.TravelTimestamp = Parameters.TimeTravelTimestamp.Value;
            }
            if (Parameters.ConsistencyLevel is { } cl)
            {
                request.ConsistencyLevel = (Grpc.ConsistencyLevel)(int)cl;
            }
            else
            {
                // Unset consistency falls back to the collection's configured level (server default), the
                // same as when no QueryParameters are provided at all.
                request.UseDefaultConsistency = true;
            }
        }
        else
        {
            request.UseDefaultConsistency = true;
        }

        return request;
    }
}
