using IO.Milvus;
using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [TestMethod]
    [TestClientProvider]
    public async Task InsertTest(IMilvusClient2 milvusClient)
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
                new FieldType("book",MilvusDataType.Int64,true),
                FieldType.CreateVarchar("book_intro",false,100L,false),
            }
        );

        bool exist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.IsTrue(exist, "Create collection failed");

        MilvusMutationResult result = await milvusClient.InsertAsync(collectionName,
            new[]
            {
                Field.Create<long>("book",new []{10L,10L,10L}),
                Field.CreateVarChar("book_intro",new []{"book1","book2","book3"})
            });
        
        Assert.IsTrue(result.InsertCount == 3, "Insert data failed");
        Assert.IsTrue(result.SuccessIndex.Count == 3, "Insert data failed");
    }
}
