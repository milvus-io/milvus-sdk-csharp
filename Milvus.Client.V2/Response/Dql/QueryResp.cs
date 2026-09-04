using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Dql;

/// <summary>
/// Represents the result of a query operation.
/// </summary>
public sealed class QueryResp
{
    private QueryResp(string collectionName, IReadOnlyList<FieldData> fieldsData)
    {
        CollectionName = collectionName;
        FieldsData = fieldsData;
    }

    internal static QueryResp FromGrpc(Grpc.QueryResults response)
        => new(response.CollectionName, DqlConversions.ProcessReturnedFieldData(response.FieldsData));

    /// <summary>
    /// The name of the queried collection.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The returned fields data.
    /// </summary>
    public IReadOnlyList<FieldData> FieldsData { get; }
}
