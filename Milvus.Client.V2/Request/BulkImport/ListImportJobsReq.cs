#pragma warning disable CS1591 // Missing XML docs
namespace Milvus.Client.V2.Requests.BulkImport;
public sealed class ListImportJobsReq
{
    public string? CollectionName { get; set; }
    public long Limit { get; set; }
    internal Grpc.ListImportTasksRequest ToGrpcListImportTasksRequest()
        => new() { CollectionName = CollectionName ?? "", Limit = Limit };
}
