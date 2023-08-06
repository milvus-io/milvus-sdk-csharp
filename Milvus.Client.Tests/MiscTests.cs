using Grpc.Core;
using Xunit;

namespace Milvus.Client.Tests;

public class MiscTests
{
    // If this test is failing for you, that means you haven't enabled authorization in Milvus; follow the instructions
    // in https://milvus.io/docs/authenticate.md.
    [Fact]
    public async Task Auth_failure_with_wrong_password()
    {
        using var badClient = new MilvusClient(
            TestEnvironment.Host, username: TestEnvironment.Username, password: "incorrect_password");

        try
        {
            await badClient.GetCollection("foo").DropAsync();

            if (Environment.GetEnvironmentVariable("CI") != null)
            {
                Assert.Fail("Authorization seems to be disabled in Milvus; follow the instructions in https://milvus.io/docs/authenticate.md");
            }
        }
        catch (RpcException) // TODO: We should maybe catch the RpcException and rethrow a MilvusException instead
        {
            // Expected behavior
        }
    }

    [Fact]
    public async Task HealthTest()
    {
        MilvusHealthState result = await Client.HealthAsync();
        Assert.True(result.IsHealthy, result.ToString());
    }

    [Fact]
    public async Task GetVersion()
        => Assert.Contains(".", await Client.GetVersionAsync());

    [Fact]
    public async Task Dispose()
    {
        var client = TestEnvironment.CreateClient();

        MilvusHealthState state = await client.HealthAsync();
        Assert.True(state.IsHealthy);

        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.HealthAsync());
    }

    private MilvusClient Client => TestEnvironment.Client;
}
