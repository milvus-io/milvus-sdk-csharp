#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Responses.BulkImport;
public sealed class ListImportJobsResp
{
    internal ListImportJobsResp(IReadOnlyList<ImportJobInfo> jobs) => Jobs = jobs;
    internal static ListImportJobsResp FromGrpc(Grpc.ListImportTasksResponse response)
        => new(response.Tasks.Select(ImportJobInfo.FromGrpc).ToList());
    public IReadOnlyList<ImportJobInfo> Jobs { get; }
}
