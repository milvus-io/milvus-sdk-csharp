#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.BulkImport;
public sealed class GetImportProgressResp
{
    internal GetImportProgressResp(ImportJobInfo job) => Job = job;
    internal static GetImportProgressResp FromGrpc(Grpc.GetImportStateResponse response)
        => new(ImportJobInfo.FromGrpc(response));
    public ImportJobInfo Job { get; }
}
