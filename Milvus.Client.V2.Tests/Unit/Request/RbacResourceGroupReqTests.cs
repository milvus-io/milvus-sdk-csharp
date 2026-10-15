using Xunit;

using Milvus.Client.V2.Requests.Rbac;
using Milvus.Client.V2.Requests.ResourceGroup;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Request;

[Trait("Category", "Unit")]
public class RbacResourceGroupReqTests
{
    [Fact]
    public void CreatePrivilegeGroup_maps_group_name()
    {
        var request = new CreatePrivilegeGroupReq { GroupName = "analytics" };

        Grpc.CreatePrivilegeGroupRequest grpc = request.ToGrpcCreatePrivilegeGroupRequest();

        Assert.Equal("analytics", grpc.GroupName);
    }

    [Fact]
    public void CreatePrivilegeGroup_throws_when_group_blank()
    {
        var request = new CreatePrivilegeGroupReq { GroupName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreatePrivilegeGroupRequest());
    }

    [Fact]
    public void CreateRole_maps_role_name()
    {
        var request = new CreateRoleReq { RoleName = "readonly", Description = "read-only role" };

        Grpc.CreateRoleRequest grpc = request.ToGrpcCreateRoleRequest();

        Assert.Equal("readonly", grpc.Entity.Name);
        Assert.Equal("read-only role", grpc.Entity.Description);
    }

    [Fact]
    public void CreateRole_throws_when_role_blank()
    {
        var request = new CreateRoleReq();
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateRoleRequest());
    }

    [Fact]
    public void CreateUser_maps_username_and_password()
    {
        var request = new CreateUserReq { UserName = "alice", Password = "secret", Description = "alice user" };

        Grpc.CreateCredentialRequest grpc = request.ToGrpcCreateCredentialRequest();

        Assert.Equal("alice", grpc.Username);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("secret")), grpc.Password);
        Assert.Equal("alice user", grpc.Description);
    }

    [Fact]
    public void CreateUser_throws_when_password_blank()
    {
        var request = new CreateUserReq { UserName = "alice" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateCredentialRequest());
    }

    [Fact]
    public void DescribeRole_maps_role_name_and_defaults_include_user_info()
    {
        var request = new DescribeRoleReq { RoleName = "readonly" };

        Grpc.SelectRoleRequest grpc = request.ToGrpcSelectRoleRequest();

        Assert.Equal("readonly", grpc.Role.Name);
        Assert.True(grpc.IncludeUserInfo);
    }

    [Fact]
    public void DescribeRole_include_user_info_false_when_opted_out()
    {
        var request = new DescribeRoleReq { RoleName = "readonly" };

        Grpc.SelectRoleRequest grpc = request.ToGrpcSelectRoleRequest(includeUserInfo: false);

        Assert.Equal("readonly", grpc.Role.Name);
        Assert.False(grpc.IncludeUserInfo);
    }

    [Fact]
    public void DescribeRole_throws_when_role_blank()
    {
        var request = new DescribeRoleReq { RoleName = "" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcSelectRoleRequest());
    }

    [Fact]
    public void DescribeUser_maps_user_name_and_defaults_include_role_info()
    {
        var request = new DescribeUserReq { UserName = "alice" };

        Grpc.SelectUserRequest grpc = request.ToGrpcSelectUserRequest();

        Assert.Equal("alice", grpc.User.Name);
        Assert.True(grpc.IncludeRoleInfo);
    }

    [Fact]
    public void DescribeUser_include_role_info_false_when_opted_out()
    {
        var request = new DescribeUserReq { UserName = "alice" };

        Grpc.SelectUserRequest grpc = request.ToGrpcSelectUserRequest(includeRoleInfo: false);

        Assert.Equal("alice", grpc.User.Name);
        Assert.False(grpc.IncludeRoleInfo);
    }

    [Fact]
    public void DescribeUser_throws_when_user_blank()
    {
        var request = new DescribeUserReq { UserName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcSelectUserRequest());
    }

    [Fact]
    public void DropPrivilegeGroup_maps_group_name()
    {
        var request = new DropPrivilegeGroupReq { GroupName = "analytics" };

        Grpc.DropPrivilegeGroupRequest grpc = request.ToGrpcDropPrivilegeGroupRequest();

        Assert.Equal("analytics", grpc.GroupName);
    }

    [Fact]
    public void DropPrivilegeGroup_throws_when_group_blank()
    {
        var request = new DropPrivilegeGroupReq { GroupName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropPrivilegeGroupRequest());
    }

    [Fact]
    public void DropRole_maps_role_name()
    {
        var request = new DropRoleReq { RoleName = "readonly" };

        Grpc.DropRoleRequest grpc = request.ToGrpcDropRoleRequest();

        Assert.Equal("readonly", grpc.RoleName);
        Assert.False(grpc.ForceDrop);
    }

    [Fact]
    public void DropRole_maps_force_drop()
    {
        var request = new DropRoleReq { RoleName = "readonly", ForceDrop = true };

        Grpc.DropRoleRequest grpc = request.ToGrpcDropRoleRequest();

        Assert.Equal("readonly", grpc.RoleName);
        Assert.True(grpc.ForceDrop);
    }

    [Fact]
    public void DescribeRole_select_grant_maps_role_and_db()
    {
        var request = new DescribeRoleReq { RoleName = "readonly", DatabaseName = "db1" };

        Grpc.SelectGrantRequest grpc = request.ToGrpcSelectGrantRequest();

        Assert.Equal("readonly", grpc.Entity.Role.Name);
        Assert.Equal("db1", grpc.Entity.DbName);
    }

    [Fact]
    public void DropRole_throws_when_role_blank()
    {
        var request = new DropRoleReq();
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropRoleRequest());
    }

    [Fact]
    public void DropUser_maps_username()
    {
        var request = new DropUserReq { UserName = "alice" };

        Grpc.DeleteCredentialRequest grpc = request.ToGrpcDeleteCredentialRequest();

        Assert.Equal("alice", grpc.Username);
    }

    [Fact]
    public void DropUser_throws_when_user_blank()
    {
        var request = new DropUserReq { UserName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDeleteCredentialRequest());
    }

    [Fact]
    public void GrantRole_maps_user_role_and_add_type()
    {
        var request = new GrantRoleReq { UserName = "alice", RoleName = "readonly" };

        Grpc.OperateUserRoleRequest grpc = request.ToGrpcOperateUserRoleRequest();

        Assert.Equal("alice", grpc.Username);
        Assert.Equal("readonly", grpc.RoleName);
        Assert.Equal(Grpc.OperateUserRoleType.AddUserToRole, grpc.Type);
    }

    [Fact]
    public void GrantRole_throws_when_role_blank()
    {
        var request = new GrantRoleReq { UserName = "alice" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcOperateUserRoleRequest());
    }

    [Fact]
    public void ListGrantsForRole_maps_role_name()
    {
        var request = new ListGrantsForRoleReq { RoleName = "readonly" };

        Grpc.SelectGrantRequest grpc = request.ToGrpcSelectGrantRequest();

        Assert.Equal("readonly", grpc.Entity.Role.Name);
    }

    [Fact]
    public void ListGrantsForRole_throws_when_role_blank()
    {
        var request = new ListGrantsForRoleReq { RoleName = "" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcSelectGrantRequest());
    }

    [Fact]
    public void ListPrivilegeGroups_builds_empty_request()
    {
        Grpc.ListPrivilegeGroupsRequest grpc = ListPrivilegeGroupsReq.ToGrpcListPrivilegeGroupsRequest();
        Assert.NotNull(grpc);
    }

    [Fact]
    public void ListRoles_builds_empty_request()
    {
        Grpc.SelectRoleRequest grpc = ListRolesReq.ToGrpcSelectRoleRequest();
        Assert.NotNull(grpc);
        // The proxy lists all roles when no role name is set (same as the Java SDK's listRoles).
        Assert.Null(grpc.Role);
        // IncludeUserInfo must be true so the server populates each role's users.
        Assert.True(grpc.IncludeUserInfo);
    }

    [Fact]
    public void ListUsers_builds_empty_request()
    {
        Grpc.ListCredUsersRequest grpc = ListUsersReq.ToGrpcListCredUsersRequest();
        Assert.NotNull(grpc);
    }

    [Fact]
    public void RevokeRole_maps_user_role_and_remove_type()
    {
        var request = new RevokeRoleReq { UserName = "alice", RoleName = "readonly" };

        Grpc.OperateUserRoleRequest grpc = request.ToGrpcOperateUserRoleRequest();

        Assert.Equal("alice", grpc.Username);
        Assert.Equal("readonly", grpc.RoleName);
        Assert.Equal(Grpc.OperateUserRoleType.RemoveUserFromRole, grpc.Type);
    }

    [Fact]
    public void RevokeRole_throws_when_user_blank()
    {
        var request = new RevokeRoleReq { RoleName = "readonly" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcOperateUserRoleRequest());
    }

    [Fact]
    public void UpdatePassword_maps_username_and_passwords()
    {
        var request = new UpdatePasswordReq
        {
            UserName = "alice",
            OldPassword = "old",
            NewPassword = "new",
            Description = "updated user"
        };

        Grpc.UpdateCredentialRequest grpc = request.ToGrpcUpdateCredentialRequest();

        Assert.Equal("alice", grpc.Username);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("old")), grpc.OldPassword);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("new")), grpc.NewPassword);
        Assert.Equal("updated user", grpc.Description);
    }

    [Fact]
    public void UpdatePassword_leaves_description_unset_when_null()
    {
        var request = new UpdatePasswordReq
        {
            UserName = "alice",
            OldPassword = "old",
            NewPassword = "new"
        };

        Grpc.UpdateCredentialRequest grpc = request.ToGrpcUpdateCredentialRequest();

        // The description field is optional on the wire; leaving it unset lets the server preserve the
        // user's existing remark instead of wiping it with an empty string.
        Assert.False(grpc.HasDescription);
    }

    [Fact]
    public void UpdatePassword_throws_when_old_password_blank()
    {
        var request = new UpdatePasswordReq { UserName = "alice", NewPassword = "new" };
        Assert.Throws<ArgumentException>(() => request.ToGrpcUpdateCredentialRequest());
    }

    [Fact]
    public void CreateResourceGroup_maps_resource_group_name()
    {
        var request = new CreateResourceGroupReq { ResourceGroupName = "__default_resource_group" };

        Grpc.CreateResourceGroupRequest grpc = request.ToGrpcCreateResourceGroupRequest();

        Assert.Equal("__default_resource_group", grpc.ResourceGroup);
        Assert.Null(grpc.Config);
    }

    [Fact]
    public void CreateResourceGroup_maps_config()
    {
        var request = new CreateResourceGroupReq
        {
            ResourceGroupName = "rg1",
            Config = new ResourceGroupConfig { RequestsNodeNum = 2, LimitsNodeNum = 4 }
        };

        Grpc.CreateResourceGroupRequest grpc = request.ToGrpcCreateResourceGroupRequest();

        Assert.Equal("rg1", grpc.ResourceGroup);
        Assert.NotNull(grpc.Config);
        Assert.Equal(2, grpc.Config.Requests.NodeNum);
        Assert.Equal(4, grpc.Config.Limits.NodeNum);
    }

    [Fact]
    public void CreateResourceGroup_throws_when_name_blank()
    {
        var request = new CreateResourceGroupReq();
        Assert.Throws<ArgumentException>(() => request.ToGrpcCreateResourceGroupRequest());
    }

    [Fact]
    public void DescribeResourceGroup_maps_resource_group_name()
    {
        var request = new DescribeResourceGroupReq { ResourceGroupName = "__default_resource_group" };

        Grpc.DescribeResourceGroupRequest grpc = request.ToGrpcDescribeResourceGroupRequest();

        Assert.Equal("__default_resource_group", grpc.ResourceGroup);
    }

    [Fact]
    public void DescribeResourceGroup_throws_when_name_blank()
    {
        var request = new DescribeResourceGroupReq { ResourceGroupName = " " };
        Assert.Throws<ArgumentException>(() => request.ToGrpcDescribeResourceGroupRequest());
    }

    [Fact]
    public void DropResourceGroup_maps_resource_group_name()
    {
        var request = new DropResourceGroupReq { ResourceGroupName = "__default_resource_group" };

        Grpc.DropResourceGroupRequest grpc = request.ToGrpcDropResourceGroupRequest();

        Assert.Equal("__default_resource_group", grpc.ResourceGroup);
    }

    [Fact]
    public void DropResourceGroup_throws_when_name_blank()
    {
        var request = new DropResourceGroupReq();
        Assert.Throws<ArgumentException>(() => request.ToGrpcDropResourceGroupRequest());
    }

    [Fact]
    public void ListResourceGroups_builds_empty_request()
    {
        Grpc.ListResourceGroupsRequest grpc = ListResourceGroupsReq.ToGrpcListResourceGroupsRequest();
        Assert.NotNull(grpc);
    }

    [Fact]
    public void TransferNode_maps_groups_and_node_count()
    {
        var request = new TransferNodeReq
        {
            SourceResourceGroup = "rg-a",
            TargetResourceGroup = "rg-b",
            NumNode = 2
        };

        Grpc.TransferNodeRequest grpc = request.ToGrpcTransferNodeRequest();

        Assert.Equal("rg-a", grpc.SourceResourceGroup);
        Assert.Equal("rg-b", grpc.TargetResourceGroup);
        Assert.Equal(2, grpc.NumNode);
    }

    [Fact]
    public void TransferNode_throws_when_source_blank()
    {
        var request = new TransferNodeReq { TargetResourceGroup = "rg-b", NumNode = 2 };
        Assert.Throws<ArgumentException>(() => request.ToGrpcTransferNodeRequest());
    }

    [Fact]
    public void TransferReplica_maps_groups_collection_and_replica_count()
    {
        var request = new TransferReplicaReq
        {
            SourceResourceGroup = "rg-a",
            TargetResourceGroup = "rg-b",
            CollectionName = "book",
            NumReplica = 3
        };

        Grpc.TransferReplicaRequest grpc = request.ToGrpcTransferReplicaRequest();

        Assert.Equal("rg-a", grpc.SourceResourceGroup);
        Assert.Equal("rg-b", grpc.TargetResourceGroup);
        Assert.Equal("book", grpc.CollectionName);
        Assert.Equal(3L, grpc.NumReplica);
    }

    [Fact]
    public void TransferReplica_throws_when_collection_blank()
    {
        var request = new TransferReplicaReq
        {
            SourceResourceGroup = "rg-a",
            TargetResourceGroup = "rg-b",
            NumReplica = 3
        };
        Assert.Throws<ArgumentException>(() => request.ToGrpcTransferReplicaRequest());
    }

    [Fact]
    public void UpdateResourceGroups_parses_json_configs()
    {
        var request = new UpdateResourceGroupsReq
        {
            ResourceGroups = new Dictionary<string, ResourceGroupConfig>
            {
                ["__default_resource_group"] = new ResourceGroupConfig
                {
                    RequestsNodeNum = 2,
                    LimitsNodeNum = 4,
                    TransferFrom = { "rg-a" },
                    TransferTo = { "rg-b" },
                    NodeFilterLabels = { ["node.label"] = "value" }
                }
            }
        };

        Grpc.UpdateResourceGroupsRequest grpc = request.ToGrpcUpdateResourceGroupsRequest();

        Grpc.ResourceGroupConfig config = grpc.ResourceGroups["__default_resource_group"];
        Assert.Equal(2, config.Requests.NodeNum);
        Assert.Equal(4, config.Limits.NodeNum);
        Grpc.ResourceGroupTransfer transferFrom = Assert.Single(config.TransferFrom);
        Assert.Equal("rg-a", transferFrom.ResourceGroup);
        Grpc.ResourceGroupTransfer transferTo = Assert.Single(config.TransferTo);
        Assert.Equal("rg-b", transferTo.ResourceGroup);
        Grpc.KeyValuePair label = Assert.Single(config.NodeFilter.NodeLabels);
        Assert.Equal("node.label", label.Key);
        Assert.Equal("value", label.Value);
    }

    [Fact]
    public void UpdateResourceGroups_throws_when_groups_empty()
    {
        var request = new UpdateResourceGroupsReq { ResourceGroups = new Dictionary<string, ResourceGroupConfig>() };
        Assert.Throws<ArgumentException>(() => request.ToGrpcUpdateResourceGroupsRequest());
    }

    [Fact]
    public void UpdateResourceGroups_throws_when_groups_null()
    {
        var request = new UpdateResourceGroupsReq { ResourceGroups = null! };
        Assert.Throws<ArgumentNullException>(() => request.ToGrpcUpdateResourceGroupsRequest());
    }
}
