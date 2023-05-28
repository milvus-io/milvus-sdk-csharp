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
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

    private string _collectionName;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously.

    public async Task InitializeAsync()
    {
        Random random = new();
        _collectionName = $"test{random.Next()}";
    }
#pragma warning restore CS1998 //

    public async Task DisposeAsync()
    {
        foreach (var client in MilvusClients)
        {
            await client.ThenDropCollectionAsync(_collectionName);
        }
        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Fact]
    public async Task AInsertTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenBookIndex(_collectionName);
        }

        await Task.Delay(1000);
    }

    [Fact]
    public async Task BDeleteTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenBookIndex(_collectionName);
            var r = await client.DeleteAsync(_collectionName, $"book_id in [1,2]");
            r.DeleteCount.Should().BeGreaterThan(1);
        }

        await Task.Delay(1000);
    }

    [Fact]
    public async Task GetCompactionStateTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenBookIndex(_collectionName);
            DetailedMilvusCollection collectionInfo = await client.DescribeCollectionAsync(_collectionName);
            long compactionId = await client.ManualCompactionAsync(collectionInfo.CollectionId);;

            compactionId.Should().NotBe(0);

            MilvusCompactionState state = await client.GetCompactionStateAsync(compactionId);
        }

        await Task.Delay(1000);
    }
}