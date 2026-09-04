using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Dql;

/// <summary>
/// Represents the result of a <c>Get</c> (fetch by primary key) operation.
/// </summary>
public sealed class GetResp
{
    private GetResp(IReadOnlyList<FieldData> fieldsData)
    {
        FieldsData = fieldsData;
    }

    internal static GetResp FromGrpc(Grpc.QueryResults response)
        => new(DqlConversions.ProcessReturnedFieldData(response.FieldsData));

    /// <summary>
    /// The returned fields data.
    /// </summary>
    public IReadOnlyList<FieldData> FieldsData { get; }
}
