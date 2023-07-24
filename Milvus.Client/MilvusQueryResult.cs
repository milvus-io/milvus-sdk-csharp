namespace Milvus.Client;

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
    public required IList<FieldData> FieldsData { get; init; }
}
