using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class HealthApiTests
{
    [Fact]
    public async Task HealthAsync_checks_server_health()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        MilvusHealthState health = await client.HealthAsync(TestContext.Current.CancellationToken);

        Assert.True(server.Service.Requests.ContainsKey("CheckHealth"));
        Assert.IsType<Milvus.Client.Grpc.CheckHealthRequest>(server.Service.Requests["CheckHealth"]);
        Assert.True(health.IsHealthy);
        Assert.Equal(MilvusErrorCode.Success, health.ErrorCode);
        Assert.Contains("healthy", health.Reasons!);
        Assert.Contains(Milvus.Client.V2.Types.QuotaState.ReadLimited, health.QuotaStates!);
    }

    [Fact]
    public async Task CheckHealthAsync_aliases_health_check()
    {
        using var server = new MockMilvusServer();
        using MilvusClientV2 client = server.CreateClient();

        MilvusHealthState health = await client.CheckHealthAsync(TestContext.Current.CancellationToken);

        Assert.True(server.Service.Requests.ContainsKey("CheckHealth"));
        Assert.True(health.IsHealthy);
    }

}
