using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class GetServerVersionTests
{
    [Fact]
    public async Task GetServerVersion_returns_version()
    {
        using var server = new MockMilvusServer { Service = { ServerVersion = "v2.6.0" } };
        using MilvusClientV2 client = server.CreateClient();

        GetServerVersionResp response = await client.GetServerVersionAsync(new GetServerVersionReq(), TestContext.Current.CancellationToken);

        Assert.Equal("v2.6.0", response.Version);
    }

    [Fact]
    public async Task GetServerVersion_detail_returns_server_info()
    {
        using var server = new MockMilvusServer { Service = { ServerVersion = "v2.6.0" } };
        using MilvusClientV2 client = server.CreateClient();

        GetServerVersionResp response = await client.GetServerVersionAsync(new GetServerVersionReq { Detail = true }, TestContext.Current.CancellationToken);

        Assert.Equal("v2.6.0", response.Version);
    }
}
