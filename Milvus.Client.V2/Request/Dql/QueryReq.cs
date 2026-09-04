using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dql;

/// <summary>
/// Represents a request to query rows from a collection by expression.
/// </summary>
public sealed class QueryReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to query.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The boolean expression identifying the rows to return (e.g. <c>"id in [1, 2, 3]"</c>).
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="QueryParameters.Ids" />; setting both throws
    /// <see cref="ArgumentException" /> at request time. When neither an expression nor ids is set, the query
    /// fetches all rows (empty filter), matching the C++/Java SDKs. For parameterized expressions, placeholders
    /// such as <c>"age &gt; {minAge}"</c> can be filled with <see cref="QueryParameters.FilterTemplates" />.
    /// </remarks>
    public string Expression { get; set; } = "";

    /// <summary>
    /// The optional query parameters.
    /// </summary>
    public QueryParameters? Parameters { get; set; }

    internal Grpc.QueryRequest ToGrpcQueryRequest(string? primaryKeyField = null)
    {
        Verify.NotNullOrWhiteSpace(CollectionName);

        bool hasIds = Parameters?.Ids is { Count: > 0 };
        bool hasExpression = !string.IsNullOrWhiteSpace(Expression);
        if (hasIds && hasExpression)
        {
            throw new ArgumentException("Expression and Ids cannot be set at the same time.");
        }

        // Neither an expression nor ids means fetch-all (empty filter), matching the C++/Java SDKs; the proxy
        // treats an empty expr as "all rows".
        if (hasIds && primaryKeyField is null)
        {
            throw new ArgumentException("Ids requires a primary key field to build the query expression.");
        }
        if (hasIds)
        {
            if (Parameters!.Ids!.Any(id => id is null))
            {
                throw new ArgumentException("Ids cannot contain null elements.");
            }

            // Reject mixed numeric/string primary keys up front (the search-by-ids path does the same), so a
            // malformed `pk in [1, "abc"]` fails fast instead of only failing server-side.
            bool hasString = Parameters!.Ids!.Any(id => id is string);
            bool hasNumeric = Parameters!.Ids!.Any(id => id is not string);
            if (hasString && hasNumeric)
            {
                throw new ArgumentException(
                    "Ids cannot mix string and numeric primary key values.");
            }
        }

        // When either Limit or Offset is set, their sum must be in [1, 16384] (the server bound), matching the
        // search path; a type-valid 0/negative limit would otherwise make the server treat the query as
        // unlimited. When both are unset the server applies its own default.
        if (Parameters is not null)
        {
            if (Parameters.Limit is < 1)
            {
                throw new ArgumentOutOfRangeException(null, Parameters.Limit,
                    "Limit must be at least 1 (a 0/negative limit makes the server treat the query as unlimited).");
            }

            if (Parameters.Offset is < 0)
            {
                throw new ArgumentOutOfRangeException(null, Parameters.Offset,
                    "Offset must be non-negative.");
            }

            // The 2.6 proxy parses offset only when a limit is provided (parseQueryParams checks
            // isLimitProvided), so an offset without a limit would be silently ignored; reject it up front.
            if (Parameters.Offset is not null && Parameters.Limit is null)
            {
                throw new ArgumentException(
                    "Offset requires Limit to be set; the server ignores an offset with no limit.");
            }

            if (Parameters.Limit is not null || Parameters.Offset is not null)
            {
                long total = (Parameters.Limit ?? 0) + (Parameters.Offset ?? 0);
                if (total < 1 || total > 16384)
                {
                    throw new ArgumentException(
                        $"The sum of Limit and Offset ({total}) must be between 1 and 16384.");
                }
            }
        }

        var request = new Grpc.QueryRequest
        {
            CollectionName = CollectionName,
            // A whitespace-only expression is treated as "no expression" (fetch-all) by hasExpression above;
            // send the trimmed empty string so the proxy does not fail parsing literal whitespace.
            Expr = hasIds ? MilvusClientV2.BuildPrimaryKeyExpression(primaryKeyField!, Parameters!.Ids!) : Expression.Trim()
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
            if (Parameters.IgnoreGrowing is not null)
            {
                request.QueryParams.Add(new Grpc.KeyValuePair
                {
                    Key = "ignore_growing",
                    Value = Parameters.IgnoreGrowing.Value ? "true" : "false"
                });
            }
            if (Parameters.Timezone is not null)
            {
                request.QueryParams.Add(new Grpc.KeyValuePair { Key = "timezone", Value = Parameters.Timezone });
            }
            foreach (KeyValuePair<string, object> template in Parameters.FilterTemplates)
            {
                request.ExprTemplateValues[template.Key] = Milvus.Client.V2.MilvusClientV2.ToTemplateValue(template.Value);
            }
            foreach (KeyValuePair<string, string> extra in Parameters.ExtraParameters)
            {
                request.QueryParams.Add(new Grpc.KeyValuePair { Key = extra.Key, Value = extra.Value });
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

        request.DbName = DatabaseName ?? "";
        return request;
    }
}
