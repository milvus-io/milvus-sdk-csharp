using Milvus.Client.V2.Requests.BulkImport;
using Milvus.Client.V2.Responses.BulkImport;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates an import job from the given data files.
    /// </summary>
    public async Task<ImportResp> ImportAsync(ImportReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ImportRequest grpcRequest = request.ToGrpcImportRequest();
        Grpc.ImportResponse response = await InvokeAsync(
            GrpcClient.ImportAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ImportResp.FromGrpc(response);
    }

    /// <summary>
    /// Gets the progress of an import job.
    /// </summary>
    public async Task<GetImportProgressResp> GetImportProgressAsync(
        GetImportProgressReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetImportStateRequest grpcRequest = request.ToGrpcGetImportStateRequest();
        Grpc.GetImportStateResponse response = await InvokeAsync(
            GrpcClient.GetImportStateAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetImportProgressResp.FromGrpc(response);
    }

    /// <summary>
    /// Lists the import jobs of a collection (or all collections when no collection name is given).
    /// </summary>
    public async Task<ListImportJobsResp> ListImportJobsAsync(
        ListImportJobsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListImportTasksRequest grpcRequest = request.ToGrpcListImportTasksRequest();
        Grpc.ListImportTasksResponse response = await InvokeAsync(
            GrpcClient.ListImportTasksAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListImportJobsResp.FromGrpc(response);
    }
}
