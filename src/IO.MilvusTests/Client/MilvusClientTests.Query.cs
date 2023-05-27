using FluentAssertions;
using IO.Milvus.Client;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async void QueryTest(IMilvusClient2 milvusClient)
    {
        string collectionName = milvusClient.GetType().Name;

        await milvusClient.CreateBookCollectionAndIndex(collectionName);
        await milvusClient.WaitLoadedAsync(collectionName);

        string expr = "book_id in [2,4,6,8]";

        var queryResult = await milvusClient.QueryAsync(
            collectionName,
            expr,
            new[] { "book_id", "word_count" });

        queryResult.FieldsData.Count.Should().Be(2);

        await milvusClient.DropCollectionAsync(collectionName);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }
}
