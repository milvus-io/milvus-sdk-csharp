using FluentAssertions;
using IO.Milvus;
using IO.Milvus.ApiSchema;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

/// <summary>
/// a test class to execute unit test about data process
/// </summary>
public sealed class DataTests : MilvusTestClientsBase, IAsyncLifetime
{
    private string collectionName;

    public async Task InitializeAsync()
    {
        Random random = new();
        collectionName = $"test{random.Next()}";
    }

    public async Task DisposeAsync()
    {
        foreach (var client in MilvusClients)
        {
            await client.ThenDropCollectionAsync(collectionName);
        }
        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Fact]
    public async Task AInsertTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenBookIndex(collectionName);
        }

        await Task.Delay(1000);
    }

    [Fact]
    public async Task BDeleteTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenBookIndex(collectionName);
            var r = await client.DeleteAsync(collectionName, $"book_id in [1,2]");
            r.DeleteCount.Should().BeGreaterThan(1);
        }

        await Task.Delay(1000);
    }

    [Fact]
    public async Task GetCompactionStateTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenBookIndex(collectionName);
            DetailedMilvusCollection collectionInfo = await client.DescribeCollectionAsync(collectionName);
            long compactionId = await client.ManualCompactionAsync(collectionInfo.CollectionId);;

            compactionId.Should().NotBe(0);

            MilvusCompactionState state = await client.GetCompactionStateAsync(compactionId);
        }

        await Task.Delay(1000);
    }
}