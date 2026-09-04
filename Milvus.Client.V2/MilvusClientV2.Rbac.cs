using Milvus.Client.V2.Requests.Rbac;
using Milvus.Client.V2.Responses.Rbac;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2;

public sealed partial class MilvusClientV2
{
    /// <summary>
    /// Creates a user with the given password.
    /// </summary>
    public async Task CreateUserAsync(CreateUserReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreateCredentialRequest grpcRequest = request.ToGrpcCreateCredentialRequest();
        await InvokeAsync(GrpcClient.CreateCredentialAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a user.
    /// </summary>
    public async Task DropUserAsync(DropUserReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DeleteCredentialRequest grpcRequest = request.ToGrpcDeleteCredentialRequest();
        await InvokeAsync(GrpcClient.DeleteCredentialAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a user's password.
    /// </summary>
    public async Task UpdatePasswordAsync(UpdatePasswordReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.UpdateCredentialRequest grpcRequest = request.ToGrpcUpdateCredentialRequest();
        await InvokeAsync(GrpcClient.UpdateCredentialAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all users.
    /// </summary>
    public async Task<ListUsersResp> ListUsersAsync(ListUsersReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListCredUsersRequest grpcRequest = ListUsersReq.ToGrpcListCredUsersRequest();
        Grpc.ListCredUsersResponse response = await InvokeAsync(
            GrpcClient.ListCredUsersAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListUsersResp.FromGrpc(response);
    }

    /// <summary>
    /// Describes a user and its roles.
    /// </summary>
    public async Task<DescribeUserResp> DescribeUserAsync(DescribeUserReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.SelectUserRequest grpcRequest = request.ToGrpcSelectUserRequest();
        Grpc.SelectUserResponse response = await InvokeAsync(
            GrpcClient.SelectUserAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return DescribeUserResp.FromGrpc(response);
    }

    /// <summary>
    /// Creates a role.
    /// </summary>
    public async Task CreateRoleAsync(CreateRoleReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreateRoleRequest grpcRequest = request.ToGrpcCreateRoleRequest();
        await InvokeAsync(GrpcClient.CreateRoleAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a role.
    /// </summary>
    public async Task DropRoleAsync(DropRoleReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DropRoleRequest grpcRequest = request.ToGrpcDropRoleRequest();
        await InvokeAsync(GrpcClient.DropRoleAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Describes a role and its users.
    /// </summary>
    public async Task<DescribeRoleResp> DescribeRoleAsync(DescribeRoleReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.SelectRoleRequest grpcRequest = request.ToGrpcSelectRoleRequest();
        Grpc.SelectRoleResponse response = await InvokeAsync(
            GrpcClient.SelectRoleAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return DescribeRoleResp.FromGrpc(response);
    }

    /// <summary>
    /// Lists all roles.
    /// </summary>
    public async Task<ListRolesResp> ListRolesAsync(ListRolesReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.SelectRoleRequest grpcRequest = ListRolesReq.ToGrpcSelectRoleRequest();
        Grpc.SelectRoleResponse response = await InvokeAsync(
            GrpcClient.SelectRoleAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListRolesResp.FromGrpc(response);
    }

    /// <summary>
    /// Adds a user to a role.
    /// </summary>
    public async Task GrantRoleAsync(GrantRoleReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.OperateUserRoleRequest grpcRequest = request.ToGrpcOperateUserRoleRequest();
        await InvokeAsync(GrpcClient.OperateUserRoleAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a user from a role.
    /// </summary>
    public async Task RevokeRoleAsync(RevokeRoleReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.OperateUserRoleRequest grpcRequest = request.ToGrpcOperateUserRoleRequest();
        await InvokeAsync(GrpcClient.OperateUserRoleAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Grants a privilege to a role.
    /// </summary>
    public async Task GrantPrivilegeAsync(GrantPrivilegeReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.OperatePrivilegeRequest grpcRequest = request.ToGrpcOperatePrivilegeRequest();
        await InvokeAsync(GrpcClient.OperatePrivilegeAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Revokes a privilege from a role.
    /// </summary>
    public async Task RevokePrivilegeAsync(RevokePrivilegeReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.OperatePrivilegeRequest grpcRequest = request.ToGrpcOperatePrivilegeRequest();
        await InvokeAsync(GrpcClient.OperatePrivilegeAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists the privileges granted to a role.
    /// </summary>
    public async Task<ListGrantsForRoleResp> ListGrantsForRoleAsync(
        ListGrantsForRoleReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.SelectGrantRequest grpcRequest = request.ToGrpcSelectGrantRequest();
        Grpc.SelectGrantResponse response = await InvokeAsync(
            GrpcClient.SelectGrantAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListGrantsForRoleResp.FromGrpc(response);
    }

    /// <summary>
    /// Creates a privilege group.
    /// </summary>
    public async Task CreatePrivilegeGroupAsync(CreatePrivilegeGroupReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.CreatePrivilegeGroupRequest grpcRequest = request.ToGrpcCreatePrivilegeGroupRequest();
        await InvokeAsync(GrpcClient.CreatePrivilegeGroupAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a privilege group.
    /// </summary>
    public async Task DropPrivilegeGroupAsync(DropPrivilegeGroupReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.DropPrivilegeGroupRequest grpcRequest = request.ToGrpcDropPrivilegeGroupRequest();
        await InvokeAsync(GrpcClient.DropPrivilegeGroupAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all privilege groups.
    /// </summary>
    public async Task<ListPrivilegeGroupsResp> ListPrivilegeGroupsAsync(
        ListPrivilegeGroupsReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.ListPrivilegeGroupsRequest grpcRequest = ListPrivilegeGroupsReq.ToGrpcListPrivilegeGroupsRequest();
        Grpc.ListPrivilegeGroupsResponse response = await InvokeAsync(
            GrpcClient.ListPrivilegeGroupsAsync, grpcRequest, static r => r.Status, cancellationToken).ConfigureAwait(false);
        return ListPrivilegeGroupsResp.FromGrpc(response);
    }

    /// <summary>
    /// Grants a privilege to a role using the v2 object/privilege model.
    /// </summary>
    public async Task GrantPrivilegeV2Async(GrantPrivilegeReqV2 request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.OperatePrivilegeV2Request grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.OperatePrivilegeV2Async, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Revokes a privilege from a role using the v2 object/privilege model.
    /// </summary>
    public async Task RevokePrivilegeV2Async(RevokePrivilegeReqV2 request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.OperatePrivilegeV2Request grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.OperatePrivilegeV2Async, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds privileges to a privilege group.
    /// </summary>
    public async Task AddPrivilegesToGroupAsync(AddPrivilegesToGroupReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.OperatePrivilegeGroupRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.OperatePrivilegeGroupAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes privileges from a privilege group.
    /// </summary>
    public async Task RemovePrivilegesFromGroupAsync(RemovePrivilegesFromGroupReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.OperatePrivilegeGroupRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.OperatePrivilegeGroupAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Alters a role's remark.
    /// </summary>
    public async Task AlterRoleAsync(AlterRoleReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.AlterRoleRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.AlterRoleAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates a user's remark.
    /// </summary>
    public async Task UpdateUserAsync(UpdateUserReq request, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(request);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        Grpc.UpdateCredentialRequest grpcRequest = request.ToGrpcRequest();
        await InvokeAsync(GrpcClient.UpdateCredentialAsync, grpcRequest, cancellationToken).ConfigureAwait(false);
    }
}
