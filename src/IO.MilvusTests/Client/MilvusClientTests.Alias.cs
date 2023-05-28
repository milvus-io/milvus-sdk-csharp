using IO.Milvus;
using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task AliasTest(IMilvusClient milvusClient)
    {
        if (milvusClient == null)
        {
            Assert.Fail("Client is null");
            return;
        }

        if (milvusClient.ToString().Contains("zilliz"))
        {
            return;
        }

        string collectionName = milvusClient.GetType().Name;
        var alias = $"{collectionName}Alias";

        //Check if collection exists.
        bool collectionExist = await milvusClient.HasCollectionAsync(collectionName);
        if (!collectionExist)
        {
            await milvusClient.CreateCollectionAsync(
                collectionName,
                new[] {
                new FieldType("book",MilvusDataType.Int64,true) }
                );
        }

        //Clear current alias which equals {collectionName}Alias.
        DetailedMilvusCollection milvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
        if (milvusCollection.Aliases?.Any(p => p == alias) == true)
        {
            await milvusClient.DropAliasAsync(alias);
        }

        //Create alias
        await milvusClient.CreateAliasAsync(collectionName,alias);

        //Check if alias created.
        milvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
        Assert.NotNull(milvusCollection);
        Assert.True(milvusCollection.Aliases?.Any(p => p == alias),$"{alias} not found, create alias failed");

        //Try to use new created alias
        DetailedMilvusCollection aliasCollection = await milvusClient.DescribeCollectionAsync(alias);
        Assert.NotNull(aliasCollection);
        Assert.Equal(alias, aliasCollection.CollectionName);

        //Drop
        await milvusClient.DropAliasAsync(alias);
        milvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
        Assert.NotNull(milvusCollection);
        Assert.False(milvusCollection.Aliases?.Any(p => p == alias) == true, $"Drop {alias} failed");

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }
}
