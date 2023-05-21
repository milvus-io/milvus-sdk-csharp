using FluentAssertions;
using IO.Milvus.Param.Dml;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Helpers;
using Xunit;

namespace IO.MilvusTests.Client;

public class QueryTest : MilvusServiceClientTestsBase, IAsyncLifetime
{
    private string collectionName;
    private string aliasName;
    private string partitionName;


    public async Task InitializeAsync()
    {
        collectionName = $"test{random.Next()}";

        aliasName = collectionName + "_aliasName";
        partitionName = collectionName + "_partitionName";

        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);
        this.GivenPartition(collectionName, partitionName);
    }

    public async Task DisposeAsync()
    {
        this.ThenDropCollection(collectionName);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Theory]
    [InlineData("book_id > 0 && book_id < 2000", 1999)]
    [InlineData("book_id in [1, 2, 3]", 3)]
    public async Task QueryDataTestWithDefaultPartition(string expr, int records)
    {
        InsertDataToBookCollection(collectionName, "_default");
        await this.GivenLoadCollectionAsync(collectionName);

        var r = MilvusClient.Query(QueryParam.Create(
            collectionName,
            new List<string> { "_default" },
            outFields: new List<string>() { "book_id", "book_name" },
            expr: expr));

        r.Assert();
        r.Data.FieldsData[0].Scalars.LongData.Data.Count.Should().Be(records);
    }

    [Theory]
    [InlineData("book_id > 0 && book_id < 2000", 1999)]
    [InlineData("book_id in [1, 2, 3]", 3)]
    public async Task QueryDataTestWithCustomPartition(string expr, int records)
    {
        InsertDataToBookCollection(collectionName, partitionName);
        await this.GivenLoadCollectionAsync(collectionName);

        var r = MilvusClient.Query(QueryParam.Create(
            collectionName,
            new List<string> { partitionName },
            outFields: new List<string>() { "book_id", "book_name" },
            expr: expr));

        r.Assert();
        r.Data.FieldsData[0].Scalars.LongData.Data.Count.Should().Be(records);
    }
}