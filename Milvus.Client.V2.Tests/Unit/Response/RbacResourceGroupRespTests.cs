using Xunit;

using Milvus.Client.V2.Responses.Rbac;
using Milvus.Client.V2.Responses.ResourceGroup;

namespace Milvus.Client.V2.Tests.Unit.Response;

[Trait("Category", "Unit")]
public class RbacResourceGroupRespTests
{
    [Fact]
    public void DescribeRole_maps_role_description_users_and_grants()
    {
        var roleResponse = new Grpc.SelectRoleResponse
        {
            Results =
            {
                new Grpc.RoleResult
                {
                    Role = new Grpc.RoleEntity { Name = "readonly", Description = "read-only role" },
                    Users = { new Grpc.UserEntity { Name = "alice" }, new Grpc.UserEntity { Name = "bob" } }
                }
            }
        };
        var grantResponse = new Grpc.SelectGrantResponse
        {
            Entities =
            {
                new Grpc.GrantEntity
                {
                    Role = new Grpc.RoleEntity { Name = "readonly" },
                    Object = new Grpc.ObjectEntity { Name = "Collection" },
                    ObjectName = "books",
                    DbName = "db1",
                    Grantor = new Grpc.GrantorEntity
                    {
                        User = new Grpc.UserEntity { Name = "admin" },
                        Privilege = new Grpc.PrivilegeEntity { Name = "Search" }
                    }
                }
            }
        };

        DescribeRoleResp resp = DescribeRoleResp.FromGrpc(roleResponse, grantResponse);

        Assert.Equal("readonly", resp.RoleName);
        Assert.Equal("read-only role", resp.Description);
        Assert.Equal(new[] { "alice", "bob" }, resp.Users);
        GrantItem grant = Assert.Single(resp.Grants);
        Assert.Equal("Collection", grant.ObjectType);
        Assert.Equal("books", grant.ObjectName);
        Assert.Equal("db1", grant.DbName);
        Assert.Equal("admin", grant.Grantor);
        Assert.Equal("Search", grant.Privilege);
    }

    [Fact]
    public void DescribeRole_handles_empty_results()
    {
        var roleResponse = new Grpc.SelectRoleResponse();
        var grantResponse = new Grpc.SelectGrantResponse();

        DescribeRoleResp resp = DescribeRoleResp.FromGrpc(roleResponse, grantResponse);

        Assert.Equal("", resp.RoleName);
        Assert.Equal("", resp.Description);
        Assert.Empty(resp.Users);
        Assert.Empty(resp.Grants);
    }

    [Fact]
    public void GrantItem_survives_missing_object_and_role()
    {
        // Singular proto messages are null in the generated C# code when absent; FromGrpc must not NRE.
        var entity = new Grpc.GrantEntity
        {
            ObjectName = "books",
            DbName = "db1"
        };

        GrantItem item = GrantItem.FromGrpc(entity);

        Assert.Equal("", item.ObjectType);
        Assert.Equal("books", item.ObjectName);
        Assert.Equal("", item.RoleName);
        Assert.Equal("", item.Grantor);
        Assert.Equal("", item.Privilege);
    }

    [Fact]
    public void RoleResult_survives_missing_role_entity()
    {
        var grpc = new Grpc.RoleResult();
        grpc.Users.Add(new Grpc.UserEntity { Name = "alice" });

        RoleResult result = RoleResult.FromGrpc(grpc);

        Assert.Equal("", result.Role);
        Assert.Equal(new[] { "alice" }, result.Users);
    }

    [Fact]
    public void UserResult_survives_missing_user_entity()
    {
        var grpc = new Grpc.UserResult();
        grpc.Roles.Add(new Grpc.RoleEntity { Name = "readonly" });

        UserResult result = UserResult.FromGrpc(grpc);

        Assert.Equal("", result.User);
        Assert.Equal(new[] { "readonly" }, result.Roles);
    }

    [Fact]
    public void DescribeUser_maps_user_and_roles()
    {
        var response = new Grpc.SelectUserResponse
        {
            Results =
            {
                new Grpc.UserResult
                {
                    User = new Grpc.UserEntity { Name = "alice" },
                    Roles = { new Grpc.RoleEntity { Name = "readonly" }, new Grpc.RoleEntity { Name = "admin" } },
                    Description = "alice user"
                }
            }
        };

        DescribeUserResp resp = DescribeUserResp.FromGrpc(response);

        Assert.NotNull(resp.User);
        Assert.Equal("alice", resp.User.User);
        Assert.Equal(new[] { "readonly", "admin" }, resp.User.Roles);
        Assert.Equal("alice user", resp.User.Description);
    }

    [Fact]
    public void DescribeUser_returns_null_user_when_no_results()
    {
        var response = new Grpc.SelectUserResponse();

        DescribeUserResp resp = DescribeUserResp.FromGrpc(response);

        Assert.Null(resp.User);
    }

    [Fact]
    public void GrantEntity_maps_role_object_db_and_privilege()
    {
        var entity = new Grpc.GrantEntity
        {
            Role = new Grpc.RoleEntity { Name = "readonly" },
            Object = new Grpc.ObjectEntity { Name = "book" },
            ObjectName = "book",
            DbName = "mydb",
            Grantor = new Grpc.GrantorEntity
            {
                Privilege = new Grpc.PrivilegeEntity { Name = "Search" },
                User = new Grpc.UserEntity { Name = "admin" }
            }
        };

        GrantEntity result = GrantEntity.FromGrpc(entity);

        Assert.Equal("readonly", result.RoleName);
        Assert.Equal("book", result.ObjectName);
        Assert.Equal("book", result.ObjectType);
        Assert.Equal("mydb", result.DbName);
        Assert.Equal("Search", result.Privilege);
        Assert.Equal("admin", result.Grantor);
    }

    [Fact]
    public void GrantEntity_defaults_missing_nested_entities()
    {
        var entity = new Grpc.GrantEntity { ObjectName = "book", DbName = "mydb" };

        GrantEntity result = GrantEntity.FromGrpc(entity);

        Assert.Equal("", result.RoleName);
        Assert.Equal("", result.ObjectType);
        Assert.Equal("", result.Privilege);
        Assert.Equal("", result.Grantor);
        Assert.Equal("book", result.ObjectName);
        Assert.Equal("mydb", result.DbName);
    }

    [Fact]
    public void ListGrantsForRole_maps_grant_entities()
    {
        var response = new Grpc.SelectGrantResponse
        {
            Entities =
            {
                new Grpc.GrantEntity
                {
                    Role = new Grpc.RoleEntity { Name = "readonly" },
                    Object = new Grpc.ObjectEntity { Name = "book" },
                    ObjectName = "book",
                    DbName = "mydb",
                    Grantor = new Grpc.GrantorEntity
                    {
                        Privilege = new Grpc.PrivilegeEntity { Name = "Search" }
                    }
                }
            }
        };

        ListGrantsForRoleResp resp = ListGrantsForRoleResp.FromGrpc(response);

        Assert.Single(resp.Grants);
        Assert.Equal("readonly", resp.Grants[0].RoleName);
        Assert.Equal("book", resp.Grants[0].ObjectName);
        Assert.Equal("Search", resp.Grants[0].Privilege);
    }

    [Fact]
    public void ListPrivilegeGroups_maps_group_names()
    {
        var response = new Grpc.ListPrivilegeGroupsResponse
        {
            PrivilegeGroups =
            {
                new Grpc.PrivilegeGroupInfo { GroupName = "analytics" },
                new Grpc.PrivilegeGroupInfo { GroupName = "reporting" }
            }
        };

        ListPrivilegeGroupsResp resp = ListPrivilegeGroupsResp.FromGrpc(response);

        Assert.Equal(new[] { "analytics", "reporting" }, resp.GroupNames);
    }

    [Fact]
    public void ListRoles_maps_role_results()
    {
        var response = new Grpc.SelectRoleResponse
        {
            Results =
            {
                new Grpc.RoleResult
                {
                    Role = new Grpc.RoleEntity { Name = "readonly" },
                    Users = { new Grpc.UserEntity { Name = "alice" } }
                }
            }
        };

        ListRolesResp resp = ListRolesResp.FromGrpc(response);

        Assert.Single(resp.Roles);
        Assert.Equal("readonly", resp.Roles[0].Role);
        Assert.Equal(new[] { "alice" }, resp.Roles[0].Users);
    }

    [Fact]
    public void ListUsers_maps_usernames()
    {
        var response = new Grpc.ListCredUsersResponse
        {
            Usernames = { "alice", "bob" }
        };

        ListUsersResp resp = ListUsersResp.FromGrpc(response);

        Assert.Equal(new[] { "alice", "bob" }, resp.Users);
    }

    [Fact]
    public void DescribeResourceGroup_maps_group_config_and_nodes()
    {
        var response = new Grpc.DescribeResourceGroupResponse
        {
            ResourceGroup = new Grpc.ResourceGroup
            {
                Name = "__default_resource_group",
                Capacity = 8,
                NumAvailableNode = 6,
                NumLoadedReplica = { ["book"] = 2, ["user"] = 1 },
                NumOutgoingNode = { ["rg-a"] = 1 },
                NumIncomingNode = { ["rg-b"] = 2 },
                Config = new Grpc.ResourceGroupConfig
                {
                    Requests = new Grpc.ResourceGroupLimit { NodeNum = 2 },
                    Limits = new Grpc.ResourceGroupLimit { NodeNum = 4 },
                    TransferFrom = { new Grpc.ResourceGroupTransfer { ResourceGroup = "rg-a" } },
                    TransferTo = { new Grpc.ResourceGroupTransfer { ResourceGroup = "rg-b" } },
                    NodeFilter = new Grpc.ResourceGroupNodeFilter
                    {
                        NodeLabels =
                        {
                            new Grpc.KeyValuePair { Key = "node-type", Value = "stateless" }
                        }
                    }
                },
                Nodes =
                {
                    new Grpc.NodeInfo { NodeId = 1, Address = "host-1:19530", Hostname = "host-1" },
                    new Grpc.NodeInfo { NodeId = 2, Address = "host-2:19530", Hostname = "host-2" }
                }
            }
        };

        DescribeResourceGroupResp resp = DescribeResourceGroupResp.FromGrpc(response);

        Assert.Equal("__default_resource_group", resp.Name);
        Assert.Equal(8, resp.Capacity);
        Assert.Equal(6, resp.NumAvailableNode);

        Assert.Equal(2, resp.NumLoadedReplica["book"]);
        Assert.Equal(1, resp.NumLoadedReplica["user"]);
        Assert.Equal(1, resp.NumOutgoingNode["rg-a"]);
        Assert.Equal(2, resp.NumIncomingNode["rg-b"]);

        Assert.Equal(2, resp.Config.RequestsNodeNum);
        Assert.Equal(4, resp.Config.LimitsNodeNum);
        Assert.Equal(new[] { "rg-a" }, resp.Config.TransferFrom);
        Assert.Equal(new[] { "rg-b" }, resp.Config.TransferTo);
        Assert.Single(resp.Config.NodeFilterLabels);
        Assert.Equal(new KeyValuePair<string, string>("node-type", "stateless"), resp.Config.NodeFilterLabels[0]);

        Assert.Equal(2, resp.Nodes.Count);
        Assert.Equal(1L, resp.Nodes[0].NodeId);
        Assert.Equal("host-1:19530", resp.Nodes[0].Address);
        Assert.Equal("host-1", resp.Nodes[0].Hostname);
        Assert.Equal(2L, resp.Nodes[1].NodeId);
        Assert.Equal("host-2", resp.Nodes[1].Hostname);
    }

    [Fact]
    public void DescribeResourceGroup_defaults_empty_config_and_nodes()
    {
        var response = new Grpc.DescribeResourceGroupResponse
        {
            ResourceGroup = new Grpc.ResourceGroup { Name = "__default_resource_group" }
        };

        DescribeResourceGroupResp resp = DescribeResourceGroupResp.FromGrpc(response);

        Assert.Null(resp.Config.RequestsNodeNum);
        Assert.Null(resp.Config.LimitsNodeNum);
        Assert.Empty(resp.Config.TransferFrom);
        Assert.Empty(resp.Config.TransferTo);
        Assert.Empty(resp.Config.NodeFilterLabels);
        Assert.Empty(resp.Nodes);
    }

    [Fact]
    public void ListResourceGroups_maps_resource_group_names()
    {
        var response = new Grpc.ListResourceGroupsResponse
        {
            ResourceGroups = { "__default_resource_group", "rg-a" }
        };

        ListResourceGroupsResp resp = ListResourceGroupsResp.FromGrpc(response);

        Assert.Equal(new[] { "__default_resource_group", "rg-a" }, resp.ResourceGroups);
    }
}
