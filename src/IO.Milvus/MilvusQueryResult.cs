using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// Milvus query result.
/// </summary>
public class MilvusQueryResult
{
    /// <summary>
    /// Collection name.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// Field data.
    /// </summary>
    public IList<Field> FieldsData { get; }
}