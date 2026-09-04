using Milvus.Client.V2.Requests.Database;
using Milvus.Client.V2.Responses.Database;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a database.
    /// </summary>
    public async Task CreateDatabaseAsync(
        CreateDatabaseReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreateDatabaseRequest grpcRequest = request.ToGrpcCreateDatabaseRequest();
        await InvokeAsync(GrpcClient.CreateDatabaseAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a database.
    /// </summary>
    public async Task DropDatabaseAsync(
        DropDatabaseReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DropDatabaseRequest grpcRequest = request.ToGrpcDropDatabaseRequest();
        await InvokeAsync(GrpcClient.DropDatabaseAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        CollectionTsCache.Instance.InvalidateDb(_endpoint, request.DatabaseName);
    }

    /// <summary>
    /// Lists all databases.
    /// </summary>
    public async Task<ListDatabasesResp> ListDatabasesAsync(
        ListDatabasesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListDatabasesRequest grpcRequest = ListDatabasesReq.ToGrpcListDatabasesRequest();
        Grpc.ListDatabasesResponse response = await InvokeAsync(
            GrpcClient.ListDatabasesAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListDatabasesResp.FromGrpc(response);
    }

    /// <summary>
    /// Describes a database.
    /// </summary>
    public async Task<DescribeDatabaseResp> DescribeDatabaseAsync(DescribeDatabaseReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DescribeDatabaseRequest grpcRequest = request.ToGrpcDescribeDatabaseRequest();
        Grpc.DescribeDatabaseResponse response = await InvokeAsync(
            GrpcClient.DescribeDatabaseAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return DescribeDatabaseResp.FromGrpc(response);
    }

    /// <summary>
    /// Alters the properties of a database.
    /// </summary>
    public async Task AlterDatabasePropertiesAsync(AlterDatabasePropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterDatabaseRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterDatabaseAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops the properties of a database.
    /// </summary>
    public async Task DropDatabasePropertiesAsync(DropDatabasePropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterDatabaseRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterDatabaseAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }
}