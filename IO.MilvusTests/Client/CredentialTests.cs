using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public class CredentialTests : IAsyncLifetime
{
    [Fact]
    public async Task Create()
    {
        // Create.
        await Client.CreateCredentialAsync("a", "bbbbbbb");
        Assert.Contains("a", await Client.ListCredUsersAsync());

        // Test if the new credential works.
        using var client = new MilvusClient(TestEnvironment.Address, "a", "bbbbbbb");
        _ = await client.HasCollectionAsync("foo");
    }

    [Fact]
    public async Task Update()
    {
        //Create
        await Client.CreateCredentialAsync("b", "ccccccc");
        Assert.Contains("b", await Client.ListCredUsersAsync());

        //Update
        await Client.UpdateCredentialAsync("b", "ccccccc", "ddddddd");

        // Test if the new credential works.
        using var client = new MilvusClient(TestEnvironment.Address, "b", "ddddddd");
        _ = await client.HasCollectionAsync("foo");
    }

    [Fact]
    public async Task Update_failed_with_wrong_old_password()
    {
        //Create
        await Client.CreateCredentialAsync("c", "ddddddd");
        Assert.Contains("c", await Client.ListCredUsersAsync());

        //Update
        var exception = await Assert.ThrowsAsync<MilvusException>(
            () => Client.UpdateCredentialAsync("c", "c", "eeeeeee"));
        Assert.Equal("UpdateCredentialFailure", exception.ErrorCode);
    }

    public async Task InitializeAsync()
    {
        //Check
        IList<string> users = await Client.ListCredUsersAsync();
        foreach (string username in new[] {"a", "b", "c"})
        {
            if (users.Contains(username))
            {
                await Client.DeleteCredentialAsync(username);
            }
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private MilvusClient Client => TestEnvironment.Client;
}
