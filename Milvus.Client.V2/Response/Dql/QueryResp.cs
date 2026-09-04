using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Dql;

/// <summary>
/// Represents the result of a query operation.
/// </summary>
public sealed class QueryResp
{
    private QueryResp(string collectionName, IReadOnlyList<FieldData> fieldsData, ulong sessionTs, string? primaryKeyName)
    {
        CollectionName = collectionName;
        FieldsData = fieldsData;
        SessionTs = sessionTs;
        PrimaryKeyName = primaryKeyName;
    }

    internal static QueryResp FromGrpc(Grpc.QueryResults response)
        => new(
            response.CollectionName,
            DqlConversions.ProcessReturnedFieldData(response.FieldsData),
            response.SessionTs,
            string.IsNullOrEmpty(response.PrimaryFieldName) ? null : response.PrimaryFieldName);

    /// <summary>
    /// The name of the queried collection.
    /// </summary>
    public string CollectionName { get; }

    /// <summary>
    /// The returned fields data.
    /// </summary>
    public IReadOnlyList<FieldData> FieldsData { get; }

    /// <summary>
    /// The name of the primary-key field of the queried collection, as reported by the server, so callers can
    /// identify the pk column in <see cref="FieldsData" /> without re-describing the collection.
    /// </summary>
    public string? PrimaryKeyName { get; }

    /// <summary>
    /// The server-side timestamp at which the query was executed, used for session-like operations such as
    /// iterators (read-your-writes consistency).
    /// </summary>
    public ulong SessionTs { get; }
}
