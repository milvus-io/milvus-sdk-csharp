using IO.Milvus.Client;
using IO.Milvus.Client.REST;
using IO.MilvusTests.Utils;
using IO.Milvus;
using Xunit;
using FluentAssertions;
using System.Security.AccessControl;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    /// <summary>
    /// https://milvus.io/docs/rbac.md
    /// </summary>
    /// <param name="milvusClient"></param>
    /// <returns></returns>
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task RBACTests(IMilvusClient milvusClient)
    {
        //Not support milvusRestClient
        if (milvusClient is MilvusRestClient || milvusClient.IsZillizCloud())
        {
            return;
        }

        //Not support below milvus 2.2.9
        MilvusVersion version = await milvusClient.GetMilvusVersionAsync();
        if (!version.GreaterThan(2, 2, 8))
        {
            return;
        }

        //username
        string username = milvusClient.GetType().Name;
        //role name
        string roleName = "roleA";

        //1.Create a user.
        //Check if the user exists.
        IList<string> users = await milvusClient.ListCredUsersAsync();
        if (users.Contains(username))
        {
            await milvusClient.DeleteCredentialAsync(username);
        }
        await milvusClient.CreateCredentialAsync(username, "abccab");

        //Check if the user exists.
        users = await milvusClient.ListCredUsersAsync();
        users.Should().Contain(username);

        //Check user role information.
        IEnumerable<MilvusUserResult> userResults = await milvusClient.SelectUserAsync(username, true);
        userResults.Should().Contain(x => x.Username == username);
        MilvusUserResult user = userResults.First(x => x.Username == username);
        if (user.Roles.Contains(roleName) && user.Roles?.Any() == true)
        {
            foreach (var userRole in user.Roles)
            {
                await milvusClient.RemoveUserFromRoleAsync(username, userRole);
            }
        }

        //2.Create a role.
        //Check if this role exist.
        IEnumerable<MilvusRoleResult> roles = await milvusClient.SelectRoleAsync(roleName, true);
        var role = roles.FirstOrDefault(x => x.RoleName == roleName);
        if (role != null && role.Users != null)
        {
            foreach (var roleUser in role.Users)
            {
                await milvusClient.RemoveUserFromRoleAsync(roleUser, roleName);
            }

            IEnumerable<MilvusGrantEntity> grantors = await milvusClient.SelectGrantForRoleAsync(roleName);
            foreach (var grantor in grantors)
            {
                await milvusClient.RevokeRolePrivilegeAsync(
                    grantor.Role,
                    grantor.Object,
                    grantor.ObjectName,
                    grantor.Grantor.Privilege);
            }
            await milvusClient.RevokeRolePrivilegeAsync(roleName, "Collection", "*", "*");
            await milvusClient.DropRoleAsync(roleName);
        }
        await milvusClient.CreateRoleAsync(roleName);

        //3.Grant a privilege to a role.
        await milvusClient.GrantRolePrivilegeAsync(
            roleName: roleName,
            @object: "Collection",
            objectName: "*",
            privilege: "Search");

        //Check if it has the privilege.
        IEnumerable<MilvusGrantEntity> milvusGrantEntities = await milvusClient.SelectGrantForRoleAsync(roleName);
        milvusGrantEntities.Should().Contain(x => x.Role == roleName);
        milvusGrantEntities.First(x => x.Role == roleName).Grantor.Privilege.Should().Be("Search");

        //4.Bind a role to a user.
        await milvusClient.AddUserToRoleAsync(username, roleName);

        //Check if user has this role.
        IEnumerable<MilvusRoleResult> roleResult = await milvusClient.SelectRoleAsync(roleName, true);
        roleResult.First(x => x.RoleName == roleName).Users.Should().Contain(username);
    }
}
