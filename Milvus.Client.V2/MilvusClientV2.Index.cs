using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Index;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates an index on a field.
    /// </summary>
    /// <param name="request">The request containing the collection/field names and index parameters.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task CreateIndexAsync(
        CreateIndexReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.CreateIndexRequest grpcRequest = request.ToGrpcCreateIndexRequest();
        await InvokeAsync(GrpcClient.CreateIndexAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops an index.
    /// </summary>
    /// <param name="request">The request containing the collection/field names and the index name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task DropIndexAsync(
        DropIndexReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.DropIndexRequest grpcRequest = request.ToGrpcDropIndexRequest();
        await InvokeAsync(GrpcClient.DropIndexAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Describes the indexes on a field of a collection.
    /// </summary>
    /// <param name="request">The request containing the collection/field names and the index name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<DescribeIndexResp> DescribeIndexAsync(
        DescribeIndexReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.DescribeIndexRequest grpcRequest = request.ToGrpcDescribeIndexRequest();
        Grpc.DescribeIndexResponse response = await InvokeAsync(
                GrpcClient.DescribeIndexAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return DescribeIndexResp.FromGrpc(response);
    }

    /// <summary>
    /// Lists the indexes of a collection.
    /// </summary>
    /// <param name="request">The request containing the collection name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<ListIndexesResp> ListIndexesAsync(
        ListIndexesReq request,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        Grpc.DescribeIndexRequest grpcRequest = request.ToGrpcDescribeIndexRequest();
        Grpc.DescribeIndexResponse response = await InvokeAsync(
                GrpcClient.DescribeIndexAsync, grpcRequest, static r => r.Status, cancellationToken)
            .ConfigureAwait(false);

        return ListIndexesResp.FromGrpc(response);
    }

    /// <summary>
    /// Alters the properties of an index.
    /// </summary>
    public async Task AlterIndexPropertiesAsync(AlterIndexPropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterIndexRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterIndexAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops the properties of an index.
    /// </summary>
    public async Task DropIndexPropertiesAsync(DropIndexPropertiesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterIndexRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterIndexAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }
}
