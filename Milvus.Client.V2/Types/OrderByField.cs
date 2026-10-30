using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// An ordering rule that sorts the returned rows by a field, either ascending or descending, carried on the
/// wire <c>order_by_fields</c> key (serialized as <c>"field:asc,field:desc"</c>).
/// </summary>
public sealed class OrderByField
{
    /// <summary>
    /// Creates a new ordering rule.
    /// </summary>
    /// <param name="fieldName">The name of the field to order by.</param>
    /// <param name="direction">The sort direction. Defaults to <see cref="AggregationDirection.Asc" />.</param>
    public OrderByField(string fieldName, AggregationDirection direction = AggregationDirection.Asc)
    {
        Verify.NotNullOrWhiteSpace(fieldName);

        FieldName = fieldName;
        Direction = direction;
    }

    /// <summary>
    /// The name of the field to order by.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The sort direction.
    /// </summary>
    public AggregationDirection Direction { get; }

    internal string ToWireString()
        => $"{FieldName}:{(Direction == AggregationDirection.Desc ? "desc" : "asc")}";
}
