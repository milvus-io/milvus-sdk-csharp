namespace IO.Milvus;

/// <summary>
/// Milvus query result.
/// </summary>
public sealed class MilvusQueryResult
{
    /// <summary>
    /// Collection name.
    /// </summary>
    public required string CollectionName { get; init; }

    /// <summary>
    /// Field data.
    /// </summary>
    public required IList<Field> FieldsData { get; init; }
}
