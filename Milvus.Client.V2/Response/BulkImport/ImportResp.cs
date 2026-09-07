#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.BulkImport;
public sealed class ImportResp
{
    internal ImportResp(IReadOnlyList<long> taskIds) => TaskIds = taskIds;
    internal static ImportResp FromGrpc(Grpc.ImportResponse response) => new(response.Tasks.ToList());
    public IReadOnlyList<long> TaskIds { get; }
}
