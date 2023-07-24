using Xunit;

namespace Milvus.Client.Tests;

public partial class MilvusClientTests
{
    [Fact]
    public async Task HealthTest()
    {
        MilvusHealthState result = await Client.HealthAsync();
        Assert.True(result.IsHealthy, result.ToString());
    }
}
