using FluentAssertions;
using IO.Milvus;
using IO.Milvus.Client;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task DeleteTest(IMilvusClient milvusClient)
    {
        string collectionName = milvusClient.GetType().Name;

        await milvusClient.CreateBookCollectionAndIndex(collectionName);

        MilvusMutationResult result = await milvusClient.DeleteAsync(collectionName, "book_id in [0,1]");

        result.DeleteCount.Should().BeGreaterThan(0);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }
}
