using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task HealthTest(IMilvusClient2 milvusClient)
    {
        bool result =await milvusClient.HealthAsync();
        Assert.True(result);
    }
}
