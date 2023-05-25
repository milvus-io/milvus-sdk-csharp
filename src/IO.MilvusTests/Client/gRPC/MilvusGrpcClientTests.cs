using IO.Milvus.Client.REST;
using Xunit;

namespace IO.MilvusTests.Client.gRPC;

public class MilvusGrpcClientTests
{
    [Fact]
    public void MilvusGrpcClientTest()
    {
        var client = new MilvusRestClient(HostConfig.Host, HostConfig.RestPort);
        Assert.NotNull(client);
    }
}
