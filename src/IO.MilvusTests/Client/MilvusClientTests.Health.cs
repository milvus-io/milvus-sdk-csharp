using IO.Milvus;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task HealthTest(IMilvusClient milvusClient)
    {
        MilvusHealthState result =await milvusClient.HealthAsync();

        Assert.True(result.IsHealthy,result.ToString());
    }
}
