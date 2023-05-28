using IO.Milvus;
using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;


public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task CollectionTest(IMilvusClient milvusClient)
    {
        string collectionName = milvusClient.GetType().Name;

        bool collectionExist = await milvusClient.HasCollectionAsync(collectionName);

        if (collectionExist)
        {
            await milvusClient.DropCollectionAsync(collectionName);
            await Task.Delay(100);//avaoid drop collection too frequently, cause error.
        }

        await milvusClient.CreateCollectionAsync(
            collectionName,
            new[] {
                new FieldType("book",MilvusDataType.Int64,true) }
            );

        IList<MilvusCollection> collections = await milvusClient.ShowCollectionsAsync();
        Assert.Contains(collections, p => p.CollectionName == collectionName);

        IDictionary<string,string> statistics  = await milvusClient.GetCollectionStatisticsAsync(collectionName);
        Assert.True(statistics.Count == 1);

        DetailedMilvusCollection detailedMilvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
        Assert.Equal(collectionName, detailedMilvusCollection.CollectionName);

        collectionExist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.True(collectionExist);

        await milvusClient.DropCollectionAsync(collectionName);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }
}
