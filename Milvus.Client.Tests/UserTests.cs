using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class UserTests(MilvusFixture milvusFixture) : IAsyncLifetime
{
    [Fact]
    public async Task Create()
    {
        await Client.CreateUserAsync(Username, "some_password");

        using var client = new MilvusClient(milvusFixture.Host, Username, "some_password", milvusFixture.Port);
        _ = await client.HasCollectionAsync("foo");
    }

    [Fact]
    public async Task List()
    {
        Assert.DoesNotContain(Username, await Client.ListUsernames());
        await Client.CreateUserAsync(Username, "some_password");
        Assert.Contains(Username, await Client.ListUsernames());
    }

    [Fact]
    public async Task Update()
    {
        await Client.CreateUserAsync(Username, "some_old_password");

        await Client.UpdatePassword(Username, "some_old_password", "some_new_password");

        using var client = new MilvusClient(milvusFixture.Host, Username, "some_new_password", milvusFixture.Port);
        _ = await client.HasCollectionAsync("foo");
    }

    [Fact]
    public async Task Update_failed_with_wrong_old_password()
    {
        await Client.CreateUserAsync(Username, "some_password");

        await Assert.ThrowsAsync<MilvusException>(
            () => Client.UpdatePassword(Username, "wrong_password", "some_new_password"));
    }

    [Fact]
    public async Task SelectRole()
    {
        Assert.Null(await Client.SelectRoleAsync(RoleName));
        Assert.DoesNotContain(await Client.SelectAllRolesAsync(), r => r.Role == RoleName);

        await Client.CreateRoleAsync(RoleName);

        RoleResult? result = await Client.SelectRoleAsync(RoleName);
        Assert.NotNull(result);
        Assert.NotNull(result.Users);
        Assert.Empty(result.Users);
        Assert.Contains(await Client.SelectAllRolesAsync(), r => r.Role == RoleName);

        await Client.CreateUserAsync(Username, "some_password");
        await Client.AddUserToRoleAsync(Username, RoleName);

        result = await Client.SelectRoleAsync(RoleName);
        Assert.Contains(result!.Users, u => u == Username);

        result = Assert.Single(await Client.SelectAllRolesAsync(), r => r.Role == RoleName);
        Assert.Contains(result.Users, u => u == Username);

        result = await Client.SelectRoleAsync(RoleName, includeUserInfo: false);
        Assert.Empty(result!.Users);

        result = Assert.Single(await Client.SelectAllRolesAsync(includeUserInfo: false), r => r.Role == RoleName);
        Assert.Empty(result.Users);
    }

    [Fact]
    public async Task SelectUser()
    {
        Assert.Null(await Client.SelectUserAsync(Username));
        Assert.DoesNotContain(await Client.SelectAllUsersAsync(), r => r.User == Username);

        await Client.CreateUserAsync(Username, "some_password");

        UserResult? result = await Client.SelectUserAsync(Username);
        Assert.NotNull(result);
        Assert.NotNull(result.Roles);
        Assert.Empty(result.Roles);
        Assert.Contains(await Client.SelectAllUsersAsync(), r => r.User == Username);

        await Client.CreateRoleAsync(RoleName);
        await Client.AddUserToRoleAsync(Username, RoleName);

        result = await Client.SelectUserAsync(Username);
        Assert.Contains(result!.Roles, r => r == RoleName);

        result = Assert.Single(await Client.SelectAllUsersAsync(), r => r.User == Username);
        Assert.Contains(result.Roles, r => r == RoleName);

        result = await Client.SelectUserAsync(Username, includeRoleInfo: false);
        Assert.Empty(result!.Roles);

        result = Assert.Single(await Client.SelectAllUsersAsync(includeRoleInfo: false), u => u.User == Username);
        Assert.Empty(result.Roles);
    }

    [Fact]
    public async Task Grant_Revoke_role_privilege()
    {
        await Client.CreateRoleAsync(RoleName);

        Assert.Empty(await Client.ListGrantsForRoleAsync(RoleName));

        await Client.GrantRolePrivilegeAsync(
            roleName: RoleName, @object: "Collection", objectName: "*", privilege: "Search");

        IReadOnlyList<GrantEntity> results = await Client.ListGrantsForRoleAsync(RoleName);

        GrantEntity result = Assert.Single(results);
        Assert.Equal("default", result.DbName);
        Assert.Equal("Collection", result.Object);
        Assert.Equal("*", result.ObjectName);
        Assert.Equal(RoleName, result.Role);
        Assert.Equal("Search", result.Grantor.Privilege);

        await Client.RevokeRolePrivilegeAsync(
            roleName: RoleName, @object: "Collection", objectName: "*", privilege: "Search");

        Assert.Empty(await Client.ListGrantsForRoleAsync(RoleName));
    }

    public async Task InitializeAsync()
    {
        RoleResult? roleResult = await Client.SelectRoleAsync(RoleName, includeUserInfo: true);
        if (roleResult is not null)
        {
            foreach (string username in roleResult.Users)
            {
                await Client.RemoveUserFromRoleAsync(username, RoleName);
            }

            foreach (GrantEntity grantEntity in await Client.ListGrantsForRoleAsync(RoleName))
            {
                await Client.RevokeRolePrivilegeAsync(
                    RoleName, grantEntity.Object, grantEntity.ObjectName, grantEntity.Grantor.Privilege);
            }

            await Client.DropRoleAsync(RoleName);
        }

        await Client.DeleteUserAsync(Username);
    }

    private const string Username = "some_user";
    private const string RoleName = "some_role";

    private readonly MilvusClient Client = milvusFixture.CreateClient();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
