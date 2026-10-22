using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// The sort direction of aggregation buckets.
/// </summary>
public enum AggregationDirection
{
    /// <summary>
    /// Ascending order.
    /// </summary>
    Asc,

    /// <summary>
    /// Descending order.
    /// </summary>
    Desc
}

/// <summary>
/// The aggregation operation of a metric in a <see cref="SearchAggregation" />.
/// </summary>
public enum AggregationMetricOp
{
    /// <summary>
    /// The average of the field values.
    /// </summary>
    Avg,

    /// <summary>
    /// The sum of the field values.
    /// </summary>
    Sum,

    /// <summary>
    /// The number of rows.
    /// </summary>
    Count,

    /// <summary>
    /// The minimum of the field values.
    /// </summary>
    Min,

    /// <summary>
    /// The maximum of the field values.
    /// </summary>
    Max
}

/// <summary>
/// A metric aggregation of a <see cref="SearchAggregation" /> bucket, e.g. <c>avg</c> of a field.
/// Mirrors the Java SDK's <c>MetricSpec</c> and the proto <c>common.MetricAggSpec</c>.
/// </summary>
public sealed class AggregationMetricSpec
{
    /// <summary>
    /// The aggregation operation.
    /// </summary>
    public AggregationMetricOp Op { get; set; }

    /// <summary>
    /// The field name the operation is applied to, or <c>"*"</c> for <see cref="AggregationMetricOp.Count" />.
    /// </summary>
    public string FieldName { get; set; } = "";

    internal Grpc.MetricAggSpec ToGrpc()
        => new()
        {
            Op = Op switch
            {
                AggregationMetricOp.Avg => "avg",
                AggregationMetricOp.Sum => "sum",
                AggregationMetricOp.Count => "count",
                AggregationMetricOp.Min => "min",
                AggregationMetricOp.Max => "max",
                _ => throw new ArgumentOutOfRangeException(nameof(Op), Op, "Unknown aggregation metric operation.")
            },
            FieldName = FieldName
        };
}

/// <summary>
/// An ordering rule of a <see cref="SearchAggregation" />, specifying the sort key (a metric alias or the
/// special keys <c>_count</c> / <c>_key</c>), the direction, and null handling. Mirrors the Java SDK's
/// <c>OrderSpec</c> and the proto <c>common.OrderSpec</c>.
/// </summary>
public sealed class AggregationOrder
{
    /// <summary>
    /// The sort key: a metric alias, or <c>_count</c> / <c>_key</c>.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// The sort direction.
    /// </summary>
    public AggregationDirection Direction { get; set; }

    /// <summary>
    /// Whether buckets with a null value sort first.
    /// </summary>
    public bool NullFirst { get; set; }

    internal Grpc.OrderSpec ToGrpc()
        => new()
        {
            Key = Key,
            Direction = Direction == AggregationDirection.Desc ? "desc" : "asc",
            NullFirst = NullFirst
        };
}

/// <summary>
/// A sort rule of an <see cref="AggregationTopHits" /> result, ordering the hits of a bucket by a field.
/// Mirrors the Java SDK's <c>SortSpec</c> and the proto <c>common.SortSpec</c>.
/// </summary>
public sealed class AggregationSort
{
    /// <summary>
    /// The field name to sort by (the special field <c>_score</c> refers to the vector similarity score).
    /// </summary>
    public string FieldName { get; set; } = "";

    /// <summary>
    /// The sort direction.
    /// </summary>
    public AggregationDirection Direction { get; set; }

    internal Grpc.SortSpec ToGrpc()
        => new()
        {
            FieldName = FieldName,
            Direction = Direction == AggregationDirection.Desc ? "desc" : "asc"
        };
}

/// <summary>
/// A document-snapshot aggregation inside a <see cref="SearchAggregation" /> bucket, returning the top hits
/// per bucket with an optional sort. Mirrors the Java SDK's <c>TopHitsSpec</c> and the proto
/// <c>common.TopHitsSpec</c>.
/// </summary>
public sealed class AggregationTopHits
{
    /// <summary>
    /// The number of top hits to return per bucket.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The sort rules applied to the hits of each bucket.
    /// </summary>
    public IList<AggregationSort> Sort { get; } = new List<AggregationSort>();

    internal Grpc.TopHitsSpec ToGrpc()
    {
        var grpc = new Grpc.TopHitsSpec { Size = Size };
        foreach (AggregationSort sort in Sort)
        {
            grpc.Sort.Add(sort.ToGrpc());
        }

        return grpc;
    }
}

/// <summary>
/// The <c>group-by</c> aggregation spec attached to a <c>search</c> request, which groups the matching
/// entities by one or more fields and optionally computes metrics, orders the groups, returns top hits, and
/// applies nested sub-aggregations. Mirrors the Java SDK's <c>SearchAggregation</c> / pymilvus's
/// <c>SearchAggregation</c> and the proto <c>common.SearchAggregationSpec</c>.
/// </summary>
public sealed class SearchAggregation
{
    /// <summary>
    /// The fields the search results are grouped by (JSON path expressions are allowed).
    /// </summary>
    public IList<string> Fields { get; } = new List<string>();

    /// <summary>
    /// The number of buckets to return at this level.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The metric aggregations keyed by their aliases (used in <see cref="AggregationOrder.Key" />).
    /// </summary>
    public IDictionary<string, AggregationMetricSpec> Metrics { get; } = new Dictionary<string, AggregationMetricSpec>(StringComparer.Ordinal);

    /// <summary>
    /// The ordering rules applied to the returned buckets, index 0 having the highest priority.
    /// </summary>
    public IList<AggregationOrder> Order { get; } = new List<AggregationOrder>();

    /// <summary>
    /// An optional top-hits aggregation returning the top documents of each bucket.
    /// </summary>
    public AggregationTopHits? TopHits { get; set; }

    /// <summary>
    /// An optional nested sub-aggregation applied within each bucket.
    /// </summary>
    public SearchAggregation? SubAggregation { get; set; }

    /// <summary>
    /// The candidate buckets searched at this level; <c>0</c> means <see cref="Size" />.
    /// </summary>
    public long SearchSize { get; set; }

    internal Grpc.SearchAggregationSpec ToGrpc()
    {
        if (Fields.Count == 0)
        {
            throw new ArgumentException("SearchAggregation.Fields must be a non-empty list.", nameof(Fields));
        }

        if (Size <= 0)
        {
            throw new ArgumentException("SearchAggregation.Size must be a positive integer.", nameof(Size));
        }

        foreach (string field in Fields)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentException("SearchAggregation.Fields must not contain empty values.", nameof(Fields));
            }
        }

        var grpc = new Grpc.SearchAggregationSpec
        {
            Size = Size,
            SearchSize = SearchSize
        };
        grpc.Fields.AddRange(Fields);
        foreach (KeyValuePair<string, AggregationMetricSpec> metric in Metrics)
        {
            grpc.Metrics[metric.Key] = metric.Value.ToGrpc();
        }

        foreach (AggregationOrder order in Order)
        {
            grpc.Order.Add(order.ToGrpc());
        }

        if (TopHits is not null)
        {
            grpc.TopHits = TopHits.ToGrpc();
        }

        if (SubAggregation is not null)
        {
            grpc.SubAggregation = SubAggregation.ToGrpc();
        }

        return grpc;
    }
}
