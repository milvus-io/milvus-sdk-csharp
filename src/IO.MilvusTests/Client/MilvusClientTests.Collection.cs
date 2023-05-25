using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;


public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
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
        Assert.Contains(collections, p => p.Name == collectionName);

        IDictionary<string,string> statistics  = await milvusClient.GetCollectionStatisticsAsync(collectionName);
        Assert.True(statistics.Count == 1);

        DetailedMilvusCollection detailedMilvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
        Assert.Equal(collectionName, detailedMilvusCollection.CollectionName);

        collectionExist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.True(collectionExist);

        await milvusClient.DropCollectionAsync(collectionName);
    }
}
