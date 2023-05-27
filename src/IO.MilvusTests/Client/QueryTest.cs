using FluentAssertions;
using IO.Milvus;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

public class QueryTest : MilvusTestClientsBase, IAsyncLifetime
{
    private string _collectionName;
    private string _partitionName;


    public async Task InitializeAsync()
    {
        Random random = new();
        _collectionName = $"test{random.Next()}";
        _partitionName = _collectionName + "_partitionName";
    }

    public async Task DisposeAsync()
    {
        foreach (var client in MilvusClients)
        {
            await client.ThenDropCollectionAsync(_collectionName);
        }

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Theory]
    [InlineData("book_id > 0 && book_id < 2000", 1999)]
    [InlineData("book_id in [1, 2, 3]", 3)]
    public async Task QueryDataTest(string expr, int records)
    {
        foreach (var client in MilvusClients)
        {
            await client.GivenBookIndex(_collectionName);
            await client.WaitLoadedAsync(_collectionName);

            MilvusQueryResult result = await client.QueryAsync(
                _collectionName,
                expr: expr,
                new[] { "book_id", "book_name" });

            var field = result.FieldsData[0];

            var bookIdResult = (result.FieldsData[0] as Field<long>);

            bookIdResult.Should().NotBeNull();
            bookIdResult.Data.Count.Should().Be(records);
        }
    }

    [Theory]
    [InlineData("book_id > 0 && book_id < 2000", 1999)]
    [InlineData("book_id in [1, 2, 3]", 3)]
    public async Task QueryDataTestWithCustomPartition(string expr, int records)
    {
        foreach (var client in MilvusClients)
        {
            if (client.IsZillizCloud())
            {
                return;
            }

            await client.GivenBookIndex(_collectionName, _partitionName);
            await client.WaitLoadedAsync(_collectionName);

            MilvusQueryResult result = await client.QueryAsync(
                _collectionName,
                expr: expr,
                new[] { "book_id", "book_name" },
                partitionNames: new[] { _partitionName });

            var bookIdResult = (result.FieldsData[0] as Field<long>);

            bookIdResult.Should().NotBeNull();
            bookIdResult.Data.Count.Should().Be(records);
        }
    }
}