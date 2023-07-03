using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus;

/// <summary>
/// Milvus query result.
/// </summary>
public sealed class MilvusQueryResult
{
    /// <summary>
    /// Collection name.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// Field data.
    /// </summary>
    public IList<Field> FieldsData { get; }

    internal static MilvusQueryResult From(Grpc.QueryResults response)
    {
        List<Field> fields = response.FieldsData.Select(Field.FromGrpcFieldData).ToList();
        return new MilvusQueryResult(response.CollectionName, fields);
    }

    internal static MilvusQueryResult From(ApiSchema.QueryResponse data)
    {
        return new MilvusQueryResult(data.CollectionName, data.FieldsData);
    }

    #region Private ==========================================================================
    private MilvusQueryResult(string collectionName, IList<Field> fields)
    {
        CollectionName = collectionName;
        FieldsData = fields;
    }
    #endregion
}