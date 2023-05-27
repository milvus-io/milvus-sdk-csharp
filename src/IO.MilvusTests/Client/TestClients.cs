using IO.Milvus.Client;
using IO.Milvus.Client.gRPC;
using Xunit;

namespace IO.MilvusTests.Client;

internal class TestClients : TheoryData<IMilvusClient2>
{
    public TestClients()
    {
        IEnumerable<MilvusConfig> configs = MilvusConfig.Load();

        foreach (var item in configs)
        {
            Add(item.CreateClient());
        }
    }
}

internal class TestClientsProvider
{

}