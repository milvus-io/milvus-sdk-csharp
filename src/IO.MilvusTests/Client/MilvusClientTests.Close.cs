using IO.Milvus;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Fact]
    public async Task DisposeTest()
    {
        var client = TestEnvironment.CreateClient();

        MilvusHealthState state = await client.HealthAsync();
        Assert.True(state.IsHealthy);

        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.HealthAsync());
    }
}
