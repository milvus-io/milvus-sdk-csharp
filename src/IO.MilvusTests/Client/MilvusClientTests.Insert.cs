using IO.Milvus;
using IO.Milvus.ApiSchema;
using IO.Milvus.Client;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
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
                FieldType.CreateVarchar("book_intro",100L),
            }
        );

        bool exist = await milvusClient.HasCollectionAsync(collectionName);
        Assert.True(exist, "Create collection failed");

        MilvusMutationResult result = await milvusClient.InsertAsync(collectionName,
            new Field[]
            {
                Field.Create<long>("book",new []{10L,10L,10L}),
                Field.CreateVarChar("book_intro",new []{"book1","book2","book3"})
            });
        
        Assert.True(result.InsertCount == 3, "Insert data failed");
        Assert.True(result.SuccessIndex.Count == 3, "Insert data failed");

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }
}
