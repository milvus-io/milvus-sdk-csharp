using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

internal class TestClients : TheoryData<IMilvusClient>
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