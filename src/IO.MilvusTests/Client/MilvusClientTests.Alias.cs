using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [TestMethod]
    [TestClientProvider]
    public async Task AliasTest(IMilvusClient2 milvusClient)
    {
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
        Assert.IsNotNull(milvusCollection);
        Assert.IsTrue(milvusCollection.Aliases?.Any(p => p == alias),$"{alias} not found, create alias failed");

        //Try to use new created alias
        DetailedMilvusCollection aliasCollection = await milvusClient.DescribeCollectionAsync(alias);
        Assert.IsNotNull(aliasCollection);
        Assert.AreEqual(alias, aliasCollection.CollectionName);

        //Drop
        await milvusClient.DropAliasAsync(alias);
        milvusCollection = await milvusClient.DescribeCollectionAsync(collectionName);
        Assert.IsNotNull(milvusCollection);
        Assert.IsFalse(milvusCollection.Aliases?.Any(p => p == alias) == true, $"Drop {alias} failed");
    }
}
