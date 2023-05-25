using IO.Milvus.Grpc;
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

    internal static MilvusQueryResult From(QueryResults response)
    {
        var fields = response.FieldsData.Select(p => Field.FromGrpcFieldData(p)).ToList();
        return new MilvusQueryResult(response.CollectionName, fields);
    }

    #region Private ==========================================================================
    private MilvusQueryResult(string collectionName, List<Field> fields)
    {
        CollectionName = collectionName;
        FieldsData = fields;
    }
    #endregion
}