using Xunit;

using Milvus.Client.V2.Requests.Rbac;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class RbacV2ReqTests
{
    [Fact]
    public void GrantPrivilegeV2_maps_role_privilege_db_and_collection()
    {
        var request = new GrantPrivilegeReqV2
        {
            RoleName = "readonly",
            Privilege = "Search",
            DatabaseName = "mydb",
            CollectionName = "book"
        };

        Grpc.OperatePrivilegeV2Request grpc = request.ToGrpcRequest();

        Assert.Equal("readonly", grpc.Role.Name);
        Assert.Equal("Search", grpc.Grantor.Privilege.Name);
        Assert.Equal("mydb", grpc.DbName);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(Grpc.OperatePrivilegeType.Grant, grpc.Type);
    }

    [Fact]
    public void RevokePrivilegeV2_uses_revoke_type()
    {
        var request = new RevokePrivilegeReqV2
        {
            RoleName = "readonly",
            Privilege = "Search",
            CollectionName = "book"
        };

        Grpc.OperatePrivilegeV2Request grpc = request.ToGrpcRequest();

        Assert.Equal("readonly", grpc.Role.Name);
        Assert.Equal("Search", grpc.Grantor.Privilege.Name);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(Grpc.OperatePrivilegeType.Revoke, grpc.Type);
    }

    [Fact]
    public void GrantPrivilegeV2_throws_when_role_blank()
    {
        var request = new GrantPrivilegeReqV2 { Privilege = "Search" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcRequest());
    }

    [Fact]
    public void AddPrivilegesToGroup_maps_group_and_privileges()
    {
        var request = new AddPrivilegesToGroupReq
        {
            GroupName = "analytics",
            Privileges = new[] { "Search", "Query" }
        };

        Grpc.OperatePrivilegeGroupRequest grpc = request.ToGrpcRequest();

        Assert.Equal("analytics", grpc.GroupName);
        Assert.Equal(Grpc.OperatePrivilegeGroupType.AddPrivilegesToGroup, grpc.Type);
        Assert.Equal(new[] { "Search", "Query" }, grpc.Privileges.Select(p => p.Name));
    }

    [Fact]
    public void RemovePrivilegesFromGroup_uses_remove_type()
    {
        var request = new RemovePrivilegesFromGroupReq
        {
            GroupName = "analytics",
            Privileges = new[] { "Search" }
        };

        Grpc.OperatePrivilegeGroupRequest grpc = request.ToGrpcRequest();

        Assert.Equal("analytics", grpc.GroupName);
        Assert.Equal(Grpc.OperatePrivilegeGroupType.RemovePrivilegesFromGroup, grpc.Type);
        Assert.Equal(new[] { "Search" }, grpc.Privileges.Select(p => p.Name));
    }

    [Fact]
    public void AddPrivilegesToGroup_throws_when_privileges_empty()
    {
        var request = new AddPrivilegesToGroupReq { GroupName = "analytics", Privileges = [] };
        Assert.Throws<ArgumentException>(() => request.ToGrpcRequest());
    }

    [Fact]
    public void AlterRole_maps_name_and_description()
    {
        var request = new AlterRoleReq { RoleName = "readonly", Description = "read-only access" };
        Grpc.AlterRoleRequest grpc = request.ToGrpcRequest();
        Assert.Equal("readonly", grpc.RoleName);
        Assert.Equal("read-only access", grpc.Description);
    }

    [Fact]
    public void UpdateUser_maps_username_and_description()
    {
        var request = new UpdateUserReq { UserName = "alice", Description = "alice's account" };
        Grpc.UpdateCredentialRequest grpc = request.ToGrpcRequest();
        Assert.Equal("alice", grpc.Username);
        Assert.Equal("alice's account", grpc.Description);
    }
}
