#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Requests.BulkImport;
public sealed class GetImportProgressReq
{
    public long TaskId { get; set; }
    internal Grpc.GetImportStateRequest ToGrpcGetImportStateRequest()
        => new() { Task = TaskId };
}
