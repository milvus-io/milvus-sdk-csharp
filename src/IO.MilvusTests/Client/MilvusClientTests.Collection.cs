using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IO.MilvusTests.Client;

[TestClass]
public class MilvusClientTests
{
    [TestMethod()]
    [TestClientProvider]
    public async Task CollectionTest(IMilvusClient2 milvusClient)
    {
        string collectionName = "Test";

        bool collectionExist = await milvusClient.HasCollectionAsync(collectionName);

        if (collectionExist)
        {
            await milvusClient.DropCollectionAsync(collectionName);
        }

        await milvusClient.CreateCollectionAsync(
            collectionName,
            ConsistencyLevel.Strong,
            new[] {
                new FieldType("book",MilvusDataType.Int64,true) }
            );

        collectionExist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.IsTrue(collectionExist);

        await milvusClient.DropCollectionAsync(collectionName);
    }
}
