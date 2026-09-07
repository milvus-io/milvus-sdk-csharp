using Milvus.Client.V2.Requests.Aliases;
using Milvus.Client.V2.Responses.Aliases;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates an alias for a collection, copying the cached session timestamp to the alias
    /// (see design doc §5.1.2).
    /// </summary>
    public async Task CreateAliasAsync(
        CreateAliasReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreateAliasRequest grpcRequest = request.ToGrpcCreateAliasRequest();
        await InvokeAsync(GrpcClient.CreateAliasAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        CollectionTsCache.Instance.Copy(_endpoint, _database, request.CollectionName, _database, request.Alias);
    }

    /// <summary>
    /// Drops an alias.
    /// </summary>
    public async Task DropAliasAsync(
        DropAliasReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DropAliasRequest grpcRequest = request.ToGrpcDropAliasRequest();
        await InvokeAsync(GrpcClient.DropAliasAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Alters an alias to point to another collection, copying the cached session timestamp.
    /// </summary>
    public async Task AlterAliasAsync(
        AlterAliasReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterAliasRequest grpcRequest = request.ToGrpcAlterAliasRequest();
        await InvokeAsync(GrpcClient.AlterAliasAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
        CollectionTsCache.Instance.Copy(_endpoint, _database, request.CollectionName, _database, request.Alias);
    }

    /// <summary>
    /// Describes an alias.
    /// </summary>
    public async Task<DescribeAliasResp> DescribeAliasAsync(
        DescribeAliasReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DescribeAliasRequest grpcRequest = request.ToGrpcDescribeAliasRequest();
        Grpc.DescribeAliasResponse response = await InvokeAsync(
            GrpcClient.DescribeAliasAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return DescribeAliasResp.FromGrpc(response);
    }

    /// <summary>
    /// Lists the aliases, optionally filtered by collection.
    /// </summary>
    public async Task<ListAliasesResp> ListAliasesAsync(
        ListAliasesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListAliasesRequest grpcRequest = request.ToGrpcListAliasesRequest();
        Grpc.ListAliasesResponse response = await InvokeAsync(
            GrpcClient.ListAliasesAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListAliasesResp.FromGrpc(response);
    }
}
