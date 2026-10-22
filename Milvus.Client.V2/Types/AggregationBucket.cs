using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// The key value of an aggregation bucket, defined by the group-by fields of one query.
/// </summary>
public sealed class AggregationBucketKey
{
    internal AggregationBucketKey(long fieldId, string fieldName, object? value)
    {
        FieldId = fieldId;
        FieldName = fieldName;
        Value = value;
    }

    /// <summary>
    /// The id of the group-by field.
    /// </summary>
    public long FieldId { get; }

    /// <summary>
    /// The name of the group-by field.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The group-by field value of the bucket (an <see cref="long" />, <see cref="string" /> or <see cref="bool" />).
    /// </summary>
    public object? Value { get; }

    internal static AggregationBucketKey FromGrpc(Grpc.BucketKeyEntry entry)
        => new(
            entry.FieldId,
            string.IsNullOrEmpty(entry.FieldName) ? entry.FieldId.ToString(System.Globalization.CultureInfo.InvariantCulture) : entry.FieldName,
            entry.ValueCase == Grpc.BucketKeyEntry.ValueOneofCase.IntVal ? entry.IntVal
                : entry.ValueCase == Grpc.BucketKeyEntry.ValueOneofCase.StringVal ? entry.StringVal
                : entry.ValueCase == Grpc.BucketKeyEntry.ValueOneofCase.BoolVal ? entry.BoolVal
                : null);
}

/// <summary>
/// One top-hit document of an aggregation bucket.
/// </summary>
public sealed class AggregationHit
{
    internal AggregationHit(object? id, float score, IReadOnlyList<AggregationHitField> fields)
    {
        Id = id;
        Score = score;
        Fields = fields;
    }

    /// <summary>
    /// The primary key of the document.
    /// </summary>
    public object? Id { get; }

    /// <summary>
    /// The relevance score of the document.
    /// </summary>
    public float Score { get; }

    /// <summary>
    /// The field values of the document.
    /// </summary>
    public IReadOnlyList<AggregationHitField> Fields { get; }

    internal static AggregationHit FromGrpc(Grpc.AggHit hit)
        => new(
            hit.PkCase == Grpc.AggHit.PkOneofCase.IntPk ? hit.IntPk
                : hit.PkCase == Grpc.AggHit.PkOneofCase.StrPk ? hit.StrPk
                : null,
            hit.Score,
            hit.Fields.Select(AggregationHitField.FromGrpc).ToArray());
}

/// <summary>
/// A single field value of an aggregation top-hit document.
/// </summary>
public sealed class AggregationHitField
{
    internal AggregationHitField(long fieldId, string fieldName, object? value)
    {
        FieldId = fieldId;
        FieldName = fieldName;
        Value = value;
    }

    /// <summary>
    /// The id of the field.
    /// </summary>
    public long FieldId { get; }

    /// <summary>
    /// The name of the field.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The field value (an <see cref="long" />, <see cref="bool" />, <see cref="float" />, <see cref="double" />,
    /// <see cref="string" /> or <see cref="byte" />[]).
    /// </summary>
    public object? Value { get; }

    internal static AggregationHitField FromGrpc(Grpc.AggHitField field)
        => new(
            field.FieldId,
            field.FieldName,
            field.ValueCase switch
            {
                Grpc.AggHitField.ValueOneofCase.IntVal => field.IntVal,
                Grpc.AggHitField.ValueOneofCase.BoolVal => field.BoolVal,
                Grpc.AggHitField.ValueOneofCase.FloatVal => field.FloatVal,
                Grpc.AggHitField.ValueOneofCase.DoubleVal => field.DoubleVal,
                Grpc.AggHitField.ValueOneofCase.StringVal => field.StringVal,
                Grpc.AggHitField.ValueOneofCase.BytesVal => field.BytesVal.ToByteArray(),
                _ => null
            });
}

/// <summary>
/// One aggregation bucket of a <c>search</c> aggregation response, keyed by the group-by field values and
/// carrying the bucket count, metric values, top hits and nested sub-buckets.
/// </summary>
public sealed class AggregationBucket
{
    internal AggregationBucket(
        IReadOnlyList<AggregationBucketKey> key, long count,
        IReadOnlyDictionary<string, object?> metrics, IReadOnlyList<AggregationHit> hits,
        IReadOnlyList<AggregationBucket> subGroups)
    {
        Key = key;
        Count = count;
        Metrics = metrics;
        Hits = hits;
        SubGroups = subGroups;
    }

    /// <summary>
    /// The bucket key values, aligned with the group-by fields.
    /// </summary>
    public IReadOnlyList<AggregationBucketKey> Key { get; }

    /// <summary>
    /// The number of documents in the bucket.
    /// </summary>
    public long Count { get; }

    /// <summary>
    /// The metric values keyed by metric alias. Values are an <see cref="long" />, <see cref="double" />,
    /// <see cref="string" /> or <see cref="bool" /> (min/max may yield string/bool when the source field has
    /// those types; count/sum/avg stay numeric).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metrics { get; }

    /// <summary>
    /// The top hits of the bucket.
    /// </summary>
    public IReadOnlyList<AggregationHit> Hits { get; }

    /// <summary>
    /// The nested sub-buckets.
    /// </summary>
    public IReadOnlyList<AggregationBucket> SubGroups { get; }

    internal static AggregationBucket FromGrpc(Grpc.AggBucket bucket)
        => new(
            bucket.Key.Select(AggregationBucketKey.FromGrpc).ToArray(),
            bucket.Count,
            bucket.Metrics.ToDictionary(
                pair => pair.Key,
                pair => ConvertMetricValue(pair.Value),
                StringComparer.Ordinal),
            bucket.Hits.Select(AggregationHit.FromGrpc).ToArray(),
            bucket.SubGroups.Select(FromGrpc).ToArray());

    private static object? ConvertMetricValue(Grpc.MetricValue value)
        => value.ValueCase switch
        {
            Grpc.MetricValue.ValueOneofCase.IntVal => value.IntVal,
            Grpc.MetricValue.ValueOneofCase.DoubleVal => value.DoubleVal,
            Grpc.MetricValue.ValueOneofCase.StringVal => value.StringVal,
            Grpc.MetricValue.ValueOneofCase.BoolVal => value.BoolVal,
            _ => null
        };
}
