using IO.Milvus;
using Xunit;
using FluentAssertions;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    /// <summary>
    /// https://milvus.io/docs/rbac.md
    /// </summary>
    /// <param name="milvusClient"></param>
    /// <returns></returns>
    [Fact]
    public async Task RBACTests()
    {
        //Not support milvusRestClient
        if (TestEnvironment.IsZillizCloud)
        {
            return;
        }

        //Not support below milvus 2.2.9
        MilvusVersion version = await Client.GetMilvusVersionAsync();
        if (!version.GreaterThan(2, 2, 8))
        {
            return;
        }

        //username
        string username = Client.GetType().Name;
        //role name
        string roleName = "roleA";

        //1.Create a user.
        //Check if the user exists.
        IList<string> users = await Client.ListCredUsersAsync();
        if (users.Contains(username))
        {
            await Client.DeleteCredentialAsync(username);
        }
        await Client.CreateCredentialAsync(username, "abccab");

        //Check if the user exists.
        users = await Client.ListCredUsersAsync();
        users.Should().Contain(username);

        //Check user role information.
        IEnumerable<MilvusUserResult> userResults = await Client.SelectUserAsync(username, true);
        userResults.Should().Contain(x => x.Username == username);
        MilvusUserResult user = userResults.First(x => x.Username == username);
        if (user.Roles.Contains(roleName) && user.Roles?.Any() == true)
        {
            foreach (var userRole in user.Roles)
            {
                await Client.RemoveUserFromRoleAsync(username, userRole);
            }
        }

        //2.Create a role.
        //Check if this role exist.
        IEnumerable<MilvusRoleResult> roles = await Client.SelectRoleAsync(roleName, true);
        var role = roles.FirstOrDefault(x => x.RoleName == roleName);
        if (role is { Users: not null })
        {
            foreach (string roleUser in role.Users)
            {
                await Client.RemoveUserFromRoleAsync(roleUser, roleName);
            }

            IEnumerable<MilvusGrantEntity> grantors = await Client.SelectGrantForRoleAsync(roleName);
            foreach (var grantor in grantors)
            {
                await Client.RevokeRolePrivilegeAsync(
                    grantor.Role,
                    grantor.Object,
                    grantor.ObjectName,
                    grantor.Grantor.Privilege);
            }
            await Client.RevokeRolePrivilegeAsync(roleName, "Collection", "*", "*");
            await Client.DropRoleAsync(roleName);
        }
        await Client.CreateRoleAsync(roleName);

        //3.Grant a privilege to a role.
        await Client.GrantRolePrivilegeAsync(
            roleName: roleName,
            @object: "Collection",
            objectName: "*",
            privilege: "Search");

        //Check if it has the privilege.
        IEnumerable<MilvusGrantEntity> milvusGrantEntities = await Client.SelectGrantForRoleAsync(roleName);
        milvusGrantEntities.Should().Contain(x => x.Role == roleName);
        milvusGrantEntities.First(x => x.Role == roleName).Grantor.Privilege.Should().Be("Search");

        //4.Bind a role to a user.
        await Client.AddUserToRoleAsync(username, roleName);

        //Check if user has this role.
        IEnumerable<MilvusRoleResult> roleResult = await Client.SelectRoleAsync(roleName, true);
        roleResult.First(x => x.RoleName == roleName).Users.Should().Contain(username);
    }
}
