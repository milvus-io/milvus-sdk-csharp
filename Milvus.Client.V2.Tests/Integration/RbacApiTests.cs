using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Rbac;
using Milvus.Client.V2.Responses.Rbac;
using Milvus.Client.Grpc;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class RbacApiTests
{
    [Fact]
    public async Task CreateUser_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateUserAsync(
            new CreateUserReq { UserName = "user1", Password = "password" },
            TestContext.Current.CancellationToken);

        CreateCredentialRequest request = Assert.IsType<CreateCredentialRequest>(server.Service.Requests["CreateCredential"]);
        Assert.Equal("user1", request.Username);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("password")), request.Password);
    }

    [Fact]
    public async Task DropUser_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropUserAsync(
            new DropUserReq { UserName = "user1" },
            TestContext.Current.CancellationToken);

        DeleteCredentialRequest request = Assert.IsType<DeleteCredentialRequest>(server.Service.Requests["DeleteCredential"]);
        Assert.Equal("user1", request.Username);
    }

    [Fact]
    public async Task UpdatePassword_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UpdatePasswordAsync(
            new UpdatePasswordReq { UserName = "user1", OldPassword = "old", NewPassword = "new" },
            TestContext.Current.CancellationToken);

        UpdateCredentialRequest request = Assert.IsType<UpdateCredentialRequest>(server.Service.Requests["UpdateCredential"]);
        Assert.Equal("user1", request.Username);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("old")), request.OldPassword);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("new")), request.NewPassword);
    }

    [Fact]
    public async Task UpdateUser_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.UpdateUserAsync(
            new UpdateUserReq { UserName = "user1", Description = "a description" },
            TestContext.Current.CancellationToken);

        UpdateCredentialRequest request = Assert.IsType<UpdateCredentialRequest>(server.Service.Requests["UpdateCredential"]);
        Assert.Equal("user1", request.Username);
        Assert.Equal("a description", request.Description);
    }

    [Fact]
    public async Task ListUsers_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        ListUsersResp response = await client.ListUsersAsync(
            new ListUsersReq(),
            TestContext.Current.CancellationToken);

        Assert.IsType<ListCredUsersRequest>(server.Service.Requests["ListCredUsers"]);
        Assert.Single(response.Users);
        Assert.Equal("root", response.Users[0]);
    }

    [Fact]
    public async Task DescribeUser_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DescribeUserAsync(
            new DescribeUserReq { UserName = "user1" },
            TestContext.Current.CancellationToken);

        SelectUserRequest request = Assert.IsType<SelectUserRequest>(server.Service.Requests["SelectUser"]);
        Assert.Equal("user1", request.User.Name);
        Assert.True(request.IncludeRoleInfo);
    }

    [Fact]
    public async Task CreateRole_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreateRoleAsync(
            new CreateRoleReq { RoleName = "role1" },
            TestContext.Current.CancellationToken);

        CreateRoleRequest request = Assert.IsType<CreateRoleRequest>(server.Service.Requests["CreateRole"]);
        Assert.Equal("role1", request.Entity.Name);
    }

    [Fact]
    public async Task DropRole_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropRoleAsync(
            new DropRoleReq { RoleName = "role1" },
            TestContext.Current.CancellationToken);

        DropRoleRequest request = Assert.IsType<DropRoleRequest>(server.Service.Requests["DropRole"]);
        Assert.Equal("role1", request.RoleName);
    }

    [Fact]
    public async Task DescribeRole_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DescribeRoleAsync(
            new DescribeRoleReq { RoleName = "role1" },
            TestContext.Current.CancellationToken);

        SelectRoleRequest request = Assert.IsType<SelectRoleRequest>(server.Service.Requests["SelectRole"]);
        Assert.Equal("role1", request.Role.Name);
        Assert.True(request.IncludeUserInfo);
    }

    [Fact]
    public async Task ListRoles_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ListRolesAsync(
            new ListRolesReq(),
            TestContext.Current.CancellationToken);

        SelectRoleRequest request = Assert.IsType<SelectRoleRequest>(server.Service.Requests["SelectRole"]);
        // The proxy lists all roles when no role name is set (same as the Java SDK's listRoles).
        Assert.Null(request.Role);
    }

    [Fact]
    public async Task GrantRole_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.GrantRoleAsync(
            new GrantRoleReq { UserName = "user1", RoleName = "role1" },
            TestContext.Current.CancellationToken);

        OperateUserRoleRequest request = Assert.IsType<OperateUserRoleRequest>(server.Service.Requests["OperateUserRole"]);
        Assert.Equal("user1", request.Username);
        Assert.Equal("role1", request.RoleName);
        Assert.Equal(OperateUserRoleType.AddUserToRole, request.Type);
    }

    [Fact]
    public async Task RevokeRole_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.RevokeRoleAsync(
            new RevokeRoleReq { UserName = "user1", RoleName = "role1" },
            TestContext.Current.CancellationToken);

        OperateUserRoleRequest request = Assert.IsType<OperateUserRoleRequest>(server.Service.Requests["OperateUserRole"]);
        Assert.Equal("user1", request.Username);
        Assert.Equal("role1", request.RoleName);
        Assert.Equal(OperateUserRoleType.RemoveUserFromRole, request.Type);
    }

    [Fact]
    public async Task GrantPrivilege_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.GrantPrivilegeAsync(
            new GrantPrivilegeReq
            {
                RoleName = "role1", Object = "Collection", ObjectName = "coll",
                Privilege = "Search", DatabaseName = "db1"
            },
            TestContext.Current.CancellationToken);

        OperatePrivilegeRequest request = Assert.IsType<OperatePrivilegeRequest>(server.Service.Requests["OperatePrivilege"]);
        Assert.Equal("role1", request.Entity.Role.Name);
        Assert.Equal("Collection", request.Entity.Object.Name);
        Assert.Equal("coll", request.Entity.ObjectName);
        Assert.Equal("Search", request.Entity.Grantor.Privilege.Name);
        Assert.Equal("db1", request.Entity.DbName);
        Assert.Equal(OperatePrivilegeType.Grant, request.Type);
    }

    [Fact]
    public async Task RevokePrivilege_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.RevokePrivilegeAsync(
            new RevokePrivilegeReq
            {
                RoleName = "role1", Object = "Collection", ObjectName = "coll",
                Privilege = "Search", DatabaseName = "db1"
            },
            TestContext.Current.CancellationToken);

        OperatePrivilegeRequest request = Assert.IsType<OperatePrivilegeRequest>(server.Service.Requests["OperatePrivilege"]);
        Assert.Equal("role1", request.Entity.Role.Name);
        Assert.Equal("Collection", request.Entity.Object.Name);
        Assert.Equal("coll", request.Entity.ObjectName);
        Assert.Equal("Search", request.Entity.Grantor.Privilege.Name);
        Assert.Equal("db1", request.Entity.DbName);
        Assert.Equal(OperatePrivilegeType.Revoke, request.Type);
    }

    [Fact]
    public async Task ListGrantsForRole_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ListGrantsForRoleAsync(
            new ListGrantsForRoleReq { RoleName = "role1" },
            TestContext.Current.CancellationToken);

        SelectGrantRequest request = Assert.IsType<SelectGrantRequest>(server.Service.Requests["SelectGrant"]);
        Assert.Equal("role1", request.Entity.Role.Name);
    }

    [Fact]
    public async Task CreatePrivilegeGroup_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.CreatePrivilegeGroupAsync(
            new CreatePrivilegeGroupReq { GroupName = "group1" },
            TestContext.Current.CancellationToken);

        CreatePrivilegeGroupRequest request = Assert.IsType<CreatePrivilegeGroupRequest>(server.Service.Requests["CreatePrivilegeGroup"]);
        Assert.Equal("group1", request.GroupName);
    }

    [Fact]
    public async Task DropPrivilegeGroup_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.DropPrivilegeGroupAsync(
            new DropPrivilegeGroupReq { GroupName = "group1" },
            TestContext.Current.CancellationToken);

        DropPrivilegeGroupRequest request = Assert.IsType<DropPrivilegeGroupRequest>(server.Service.Requests["DropPrivilegeGroup"]);
        Assert.Equal("group1", request.GroupName);
    }

    [Fact]
    public async Task ListPrivilegeGroups_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.ListPrivilegeGroupsAsync(
            new ListPrivilegeGroupsReq(),
            TestContext.Current.CancellationToken);

        Assert.IsType<ListPrivilegeGroupsRequest>(server.Service.Requests["ListPrivilegeGroups"]);
    }

    [Fact]
    public async Task GrantPrivilegeV2_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.GrantPrivilegeV2Async(
            new GrantPrivilegeReqV2 { RoleName = "role1", Privilege = "Search", DatabaseName = "db", CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        OperatePrivilegeV2Request request = Assert.IsType<OperatePrivilegeV2Request>(server.Service.Requests["OperatePrivilegeV2"]);
        Assert.Equal("role1", request.Role.Name);
        Assert.Equal("Search", request.Grantor.Privilege.Name);
        Assert.Equal("db", request.DbName);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal(OperatePrivilegeType.Grant, request.Type);
    }

    [Fact]
    public async Task RevokePrivilegeV2_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.RevokePrivilegeV2Async(
            new RevokePrivilegeReqV2 { RoleName = "role1", Privilege = "Search", DatabaseName = "db", CollectionName = "coll" },
            TestContext.Current.CancellationToken);

        OperatePrivilegeV2Request request = Assert.IsType<OperatePrivilegeV2Request>(server.Service.Requests["OperatePrivilegeV2"]);
        Assert.Equal("role1", request.Role.Name);
        Assert.Equal("Search", request.Grantor.Privilege.Name);
        Assert.Equal("db", request.DbName);
        Assert.Equal("coll", request.CollectionName);
        Assert.Equal(OperatePrivilegeType.Revoke, request.Type);
    }

    [Fact]
    public async Task AddPrivilegesToGroup_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.AddPrivilegesToGroupAsync(
            new AddPrivilegesToGroupReq { GroupName = "group1", Privileges = new[] { "Search", "Query" } },
            TestContext.Current.CancellationToken);

        OperatePrivilegeGroupRequest request = Assert.IsType<OperatePrivilegeGroupRequest>(server.Service.Requests["OperatePrivilegeGroup"]);
        Assert.Equal("group1", request.GroupName);
        Assert.Equal(OperatePrivilegeGroupType.AddPrivilegesToGroup, request.Type);
        Assert.Equal(new[] { "Search", "Query" }, request.Privileges.Select(p => p.Name));
    }

    [Fact]
    public async Task RemovePrivilegesFromGroup_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.RemovePrivilegesFromGroupAsync(
            new RemovePrivilegesFromGroupReq { GroupName = "group1", Privileges = new[] { "Search", "Query" } },
            TestContext.Current.CancellationToken);

        OperatePrivilegeGroupRequest request = Assert.IsType<OperatePrivilegeGroupRequest>(server.Service.Requests["OperatePrivilegeGroup"]);
        Assert.Equal("group1", request.GroupName);
        Assert.Equal(OperatePrivilegeGroupType.RemovePrivilegesFromGroup, request.Type);
        Assert.Equal(new[] { "Search", "Query" }, request.Privileges.Select(p => p.Name));
    }

    [Fact]
    public async Task AlterRole_forwards_request()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        await client.AlterRoleAsync(
            new AlterRoleReq { RoleName = "role1", Description = "a description" },
            TestContext.Current.CancellationToken);

        AlterRoleRequest request = Assert.IsType<AlterRoleRequest>(server.Service.Requests["AlterRole"]);
        Assert.Equal("role1", request.RoleName);
        Assert.Equal("a description", request.Description);
    }
}
