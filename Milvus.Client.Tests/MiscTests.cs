using Grpc.Core;
using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class MiscTests(MilvusFixture milvusFixture) : IDisposable
{
    // If this test is failing for you, that means you haven't enabled authorization in Milvus; follow the instructions
    // in https://milvus.io/docs/authenticate.md.
    [Fact]
    public async Task Auth_failure_with_wrong_password()
    {
        using var badClient = new MilvusClient(
            milvusFixture.Host, username: milvusFixture.Username, password: "incorrect_password");

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
    {
        string version =  await Client.GetVersionAsync();

        Assert.NotEmpty(version);
    }

    [Fact]
    public async Task Dispose_client()
    {
        MilvusHealthState state = await Client.HealthAsync();
        Assert.True(state.IsHealthy);

        Client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => Client.HealthAsync());
      }

    private readonly MilvusClient Client = milvusFixture.CreateClient();

    public void Dispose() => Client.Dispose();
}
