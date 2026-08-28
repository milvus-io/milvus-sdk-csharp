using Xunit;

namespace Milvus.Client.Tests;

public class UserTests(MilvusFixture milvusFixture) : IAsyncLifetime
{
    [Fact]
    public async Task Create()
    {
        await Client.CreateUserAsync(Username, "some_password", TestContext.Current.CancellationToken);

        using var client = new MilvusClient(milvusFixture.Host, Username, "some_password", milvusFixture.Port);
        _ = await client.HasCollectionAsync("foo", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task List()
    {
        Assert.DoesNotContain(Username, await Client.ListUsernames(TestContext.Current.CancellationToken));
        await Client.CreateUserAsync(Username, "some_password", TestContext.Current.CancellationToken);
        Assert.Contains(Username, await Client.ListUsernames(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update()
    {
        await Client.CreateUserAsync(Username, "some_old_password", TestContext.Current.CancellationToken);

        await Client.UpdatePassword(Username, "some_old_password", "some_new_password", TestContext.Current.CancellationToken);

        using var client = new MilvusClient(milvusFixture.Host, Username, "some_new_password", milvusFixture.Port);
        _ = await client.HasCollectionAsync("foo", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_failed_with_wrong_old_password()
    {
        await Client.CreateUserAsync(Username, "some_password", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<MilvusException>(
            () => Client.UpdatePassword(Username, "wrong_password", "some_new_password", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SelectRole()
    {
        Assert.Null(await Client.SelectRoleAsync(RoleName, cancellationToken: TestContext.Current.CancellationToken));
        Assert.DoesNotContain(await Client.SelectAllRolesAsync(cancellationToken: TestContext.Current.CancellationToken), r => r.Role == RoleName);

        await Client.CreateRoleAsync(RoleName, TestContext.Current.CancellationToken);

        RoleResult? result = await Client.SelectRoleAsync(RoleName, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Users);
        Assert.Empty(result.Users);
        Assert.Contains(await Client.SelectAllRolesAsync(cancellationToken: TestContext.Current.CancellationToken), r => r.Role == RoleName);

        await Client.CreateUserAsync(Username, "some_password", TestContext.Current.CancellationToken);
        await Client.AddUserToRoleAsync(Username, RoleName, TestContext.Current.CancellationToken);

        result = await Client.SelectRoleAsync(RoleName, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result!.Users, u => u == Username);

        result = Assert.Single(await Client.SelectAllRolesAsync(cancellationToken: TestContext.Current.CancellationToken), r => r.Role == RoleName);
        Assert.Contains(result.Users, u => u == Username);

        result = await Client.SelectRoleAsync(RoleName, includeUserInfo: false, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(result!.Users);

        result = Assert.Single(await Client.SelectAllRolesAsync(includeUserInfo: false, cancellationToken: TestContext.Current.CancellationToken), r => r.Role == RoleName);
        Assert.Empty(result.Users);
    }

    [Fact]
    public async Task SelectUser()
    {
        Assert.Null(await Client.SelectUserAsync(Username, cancellationToken: TestContext.Current.CancellationToken));
        Assert.DoesNotContain(await Client.SelectAllUsersAsync(cancellationToken: TestContext.Current.CancellationToken), r => r.User == Username);

        await Client.CreateUserAsync(Username, "some_password", TestContext.Current.CancellationToken);

        UserResult? result = await Client.SelectUserAsync(Username, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Roles);
        Assert.Empty(result.Roles);
        Assert.Contains(await Client.SelectAllUsersAsync(cancellationToken: TestContext.Current.CancellationToken), r => r.User == Username);

        await Client.CreateRoleAsync(RoleName, TestContext.Current.CancellationToken);
        await Client.AddUserToRoleAsync(Username, RoleName, TestContext.Current.CancellationToken);

        result = await Client.SelectUserAsync(Username, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result!.Roles, r => r == RoleName);

        result = Assert.Single(await Client.SelectAllUsersAsync(cancellationToken: TestContext.Current.CancellationToken), r => r.User == Username);
        Assert.Contains(result.Roles, r => r == RoleName);

        result = await Client.SelectUserAsync(Username, includeRoleInfo: false, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(result!.Roles);

        result = Assert.Single(await Client.SelectAllUsersAsync(includeRoleInfo: false, cancellationToken: TestContext.Current.CancellationToken), u => u.User == Username);
        Assert.Empty(result.Roles);
    }

    [Fact]
    public async Task Grant_Revoke_role_privilege()
    {
        await Client.CreateRoleAsync(RoleName, TestContext.Current.CancellationToken);

        Assert.Empty(await Client.ListGrantsForRoleAsync(RoleName, TestContext.Current.CancellationToken));

        await Client.GrantRolePrivilegeAsync(
            roleName: RoleName, @object: "Collection", objectName: "*", privilege: "Search", cancellationToken: TestContext.Current.CancellationToken);

        IReadOnlyList<GrantEntity> results = await Client.ListGrantsForRoleAsync(RoleName, TestContext.Current.CancellationToken);

        GrantEntity result = Assert.Single(results);
        Assert.Equal("default", result.DbName);
        Assert.Equal("Collection", result.Object);
        Assert.Equal("*", result.ObjectName);
        Assert.Equal(RoleName, result.Role);
        Assert.Equal("Search", result.Grantor.Privilege);

        await Client.RevokeRolePrivilegeAsync(
            roleName: RoleName, @object: "Collection", objectName: "*", privilege: "Search", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(await Client.ListGrantsForRoleAsync(RoleName, TestContext.Current.CancellationToken));
    }

    public async ValueTask InitializeAsync()
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

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
