using FluentAssertions;
using IO.Milvus;
using IO.Milvus.Client.REST;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

public sealed class CollectionTest : MilvusTestClientsBase, IAsyncLifetime
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    private string _collectionName;
    private string _aliasName;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously.
    public async Task InitializeAsync()
    {
        Random random = new();
        _collectionName = $"test{random.Next()}";
        _aliasName = _collectionName + "_aliasName";
    }
#pragma warning restore CS1998 //

    public async Task DisposeAsync()
    {
        foreach (var client in MilvusClients)
        {
            await client.DropCollectionAsync(_collectionName);
        }

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Fact]
    public async Task HasCollectionTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenCollection(_collectionName);
            bool value = await client.HasCollectionAsync(_collectionName);
            value.Should().BeTrue();
        }
        await Task.Delay(1000);
    }

    [Fact]
    public async void NotHasCollectionTest()
    {
        foreach (var client in MilvusClients)
        {
            bool r = await client.HasCollectionAsync("xxxxxxx");
            r.Should().BeFalse();
        }
        await Task.Delay(1000);
    }

    [Fact]
    public async Task CreateCollectionTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.CreateBookCollectionAsync(_collectionName);
            bool r = await client.HasCollectionAsync(_collectionName);
            r.Should().BeTrue();
        }
        await Task.Delay(1000);
    }

    [Fact]
    public async Task ShowCollectionsTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenNoCollection(_collectionName);
            await client.CreateBookCollectionAndIndex(_collectionName);

            IList<MilvusCollection> collections = await client.ShowCollectionsAsync();

            collections.Select(_ => _.CollectionName).Should().Contain(_collectionName);
        }
        await Task.Delay(1000);
    }

    [Fact]
    public async Task DescribeCollectionTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenNoCollection(_collectionName);
            await client.CreateBookCollectionAndIndex(_collectionName);

            DetailedMilvusCollection collectionInfo = await client.DescribeCollectionAsync(_collectionName);

            collectionInfo.Schema.Name.Should().Be(_collectionName);
        }
        await Task.Delay(1000);
    }

    [Fact]
    public async Task GetCollectionStatisticsTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenNoCollection(_collectionName);
            await client.CreateBookCollectionAndIndex(_collectionName);

            IDictionary<string, string> statistics = await client.GetCollectionStatisticsAsync(collectionName: _collectionName);

            statistics.First().Key.Should().Be("row_count");
        }
        await Task.Delay(1000);
    }

    [Fact]
    public async Task LoadCollectionTest()
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenNoCollection(_collectionName);
            await client.CreateBookCollectionAndIndex(_collectionName);

            await client.LoadCollectionAsync(collectionName: _collectionName);
            await client.WaitLoadedAsync(_collectionName);

            if (client is not MilvusRestClient)
            {
                (await client.GetLoadingProgressAsync(_collectionName)).Should().Be(100);
            }

            await client.ReleaseCollectionAsync(_collectionName);
        }
        await Task.Delay(1000);
    }
}