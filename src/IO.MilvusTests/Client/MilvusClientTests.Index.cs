using IO.Milvus.Client;
using Xunit;
using IO.MilvusTests.Utils;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task IndexTest(IMilvusClient milvusClient)
    {
        string collectionName = milvusClient.GetType().Name;

        await milvusClient.CreateBookCollectionAndIndex(collectionName);

        await milvusClient.DropCollectionAsync(collectionName);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }
}
