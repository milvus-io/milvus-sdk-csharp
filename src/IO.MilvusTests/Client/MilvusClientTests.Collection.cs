using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IO.MilvusTests.Client;

[TestClass]
public partial class MilvusClientTests
{
    [TestMethod()]
    [TestClientProvider]
    public async Task CollectionTest(IMilvusClient2 milvusClient)
    {
        string collectionName = milvusClient.GetType().Name;

        bool collectionExist = await milvusClient.HasCollectionAsync(collectionName);

        if (collectionExist)
        {
            await milvusClient.DropCollectionAsync(collectionName);
        }

        await milvusClient.CreateCollectionAsync(
            collectionName,
            new[] {
                new FieldType("book",MilvusDataType.Int64,true) }
            );

        IList<MilvusCollection> collections = await milvusClient.ShowCollectionsAsync();
        Assert.IsTrue(collections.Any(p => p.Name == collectionName));

        IDictionary<string,string> statistics  = await milvusClient.GetCollectionStatisticsAsync(collectionName);
        Assert.IsTrue(statistics.Count == 1);

        DetailedMilvusCollection detailedMilvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
        Assert.AreEqual(collectionName, detailedMilvusCollection.CollectionName);

        collectionExist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.IsTrue(collectionExist);

        await milvusClient.DropCollectionAsync(collectionName);
    }
}
