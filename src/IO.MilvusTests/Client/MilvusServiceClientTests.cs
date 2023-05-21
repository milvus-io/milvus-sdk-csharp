using FluentAssertions;
using IO.Milvus.Client;
using IO.Milvus.Param;
using IO.MilvusTests.Client.Base;
using Xunit;

namespace IO.MilvusTests.Client;

public class MilvusServiceClientTests : MilvusServiceClientTestsBase
{
    [Fact]
    public void MilvusServiceClientTest()
    {
        var service = DefaultClient();

        service.Should().NotBeNull();
        service.ClientIsReady().Should().BeTrue();
    }

    [Fact]
    public void CloseTest()
    {
        var service = NewClient();
        service.Close();
    }

    [Fact]
    public async Task CloseAsyncTest()
    {
        var service = NewClient();
        await service.CloseAsync();
    }

    [Fact]
    public void CreateGrpcDefaultClientTest()
    {
        var defaultClient = MilvusServiceClient.CreateGrpcDefaultClient(ConnectParam.Create(
            HostConfig.Host,
            HostConfig.Port
        ));

        defaultClient.Should().NotBeNull();
    }

    [Fact]
    public void ConnectParam_Create()
    {
        var connectParam = ConnectParam.Create(
            "somehost",
            11111,
            name: "username",
            password: "password",
            useHttps: true
        );

        connectParam.Authorization.Should().Be("username:password");
        connectParam.Host.Should().Be("somehost");
        connectParam.Port.Should().Be(11111);
        connectParam.UseHttps.Should().Be(true);
    }
}