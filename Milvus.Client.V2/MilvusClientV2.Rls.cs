using Milvus.Client.V2.Requests.Rls;
using Milvus.Client.V2.Responses.Rls;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a row policy for the specified collection. An existing policy name is an error.
    /// </summary>
    /// <remarks>
    /// The policy type accepts permissive or restrictive, and the actions control which operations the policy
    /// applies to. The expressions are validated by Milvus.
    /// </remarks>
    /// <param name="request">The create row policy request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task CreateRowPolicyAsync(CreateRowPolicyReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreateRowPolicyRequest grpcRequest = request.ToGrpcCreateRowPolicyRequest();
        await InvokeAsync(GrpcClient.CreateRowPolicyAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces the definition of the named policy, preserving its server-side identity. The policy must exist;
    /// all definition fields replace their old values, and omitted expressions and description become empty
    /// strings.
    /// </summary>
    /// <param name="request">The update row policy request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task UpdateRowPolicyAsync(UpdateRowPolicyReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.UpdateRowPolicyRequest grpcRequest = request.ToGrpcUpdateRowPolicyRequest();
        await InvokeAsync(GrpcClient.UpdateRowPolicyAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a row policy by name. Dropping a missing policy succeeds.
    /// </summary>
    /// <param name="request">The drop row policy request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task DropRowPolicyAsync(DropRowPolicyReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DropRowPolicyRequest grpcRequest = request.ToGrpcDropRowPolicyRequest();
        await InvokeAsync(GrpcClient.DropRowPolicyAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists the row policies of the specified collection.
    /// </summary>
    /// <param name="request">The list row policies request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<ListRowPoliciesResp> ListRowPoliciesAsync(
        ListRowPoliciesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListRowPoliciesRequest grpcRequest = request.ToGrpcListRowPoliciesRequest();
        Grpc.ListRowPoliciesResponse response = await InvokeAsync(
            GrpcClient.ListRowPoliciesAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListRowPoliciesResp.FromGrpc(response);
    }

    /// <summary>
    /// Incrementally upserts a non-empty mapping of principal tags.
    /// </summary>
    /// <remarks>
    /// Values may be strings, finite numbers, or one-dimensional arrays of strings or numbers. Keys omitted from
    /// this call retain their values.
    /// </remarks>
    /// <param name="request">The set RLS principal tags request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task SetRLSPrincipalTagsAsync(SetRLSPrincipalTagsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.SetRLSPrincipalTagsRequest grpcRequest = request.ToGrpcSetRLSPrincipalTagsRequest();
        await InvokeAsync(GrpcClient.SetRLSPrincipalTagsAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the decoded tags of the specified principal.
    /// </summary>
    /// <param name="request">The get RLS principal tags request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<GetRLSPrincipalTagsResp> GetRLSPrincipalTagsAsync(
        GetRLSPrincipalTagsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.GetRLSPrincipalTagsRequest grpcRequest = request.ToGrpcGetRLSPrincipalTagsRequest();
        Grpc.GetRLSPrincipalTagsResponse response = await InvokeAsync(
            GrpcClient.GetRLSPrincipalTagsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return GetRLSPrincipalTagsResp.FromGrpc(response);
    }

    /// <summary>
    /// Lists the application principal names with stored tags in the specified collection.
    /// </summary>
    /// <param name="request">The list RLS principals request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task<ListRLSPrincipalsResp> ListRLSPrincipalsAsync(
        ListRLSPrincipalsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListRLSPrincipalsRequest grpcRequest = request.ToGrpcListRLSPrincipalsRequest();
        Grpc.ListRLSPrincipalsResponse response = await InvokeAsync(
            GrpcClient.ListRLSPrincipalsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListRLSPrincipalsResp.FromGrpc(response);
    }

    /// <summary>
    /// Deletes the selected tag keys of a principal, or all tags when the tag key list is empty.
    /// </summary>
    /// <param name="request">The delete RLS principal tags request.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public async Task DeleteRLSPrincipalTagsAsync(
        DeleteRLSPrincipalTagsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DeleteRLSPrincipalTagsRequest grpcRequest = request.ToGrpcDeleteRLSPrincipalTagsRequest();
        await InvokeAsync(GrpcClient.DeleteRLSPrincipalTagsAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }
}
