using FluentAssertions;
using IO.Milvus.Param;
using IO.Milvus.Param.Collection;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Helpers;
using Xunit;

namespace IO.MilvusTests.Client;

public sealed class CollectionTest : MilvusServiceClientTestsBase, IAsyncLifetime
{
    private string collectionName;
    private string aliasName;

    public async Task InitializeAsync()
    {
        collectionName = $"test{random.Next()}";

        aliasName = collectionName + "_aliasName";
    }

    public async Task DisposeAsync()
    {
        this.ThenDropCollection(collectionName);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Fact]
    public async Task HasCollectionTest()
    {
        await this.GivenCollection(collectionName);

        var r = MilvusClient.HasCollection(collectionName);

        r.ShouldSuccess();
        r.Data.Should().BeTrue();
    }

    [Fact]
    public void NotHasCollectionTest()
    {
        var r = MilvusClient.HasCollection("xxxxxxx");

        r.ShouldSuccess();
        r.Data.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCollectionTest()
    {
        var r = await CreateBookCollectionAsync(collectionName);
        r.ShouldSuccess();

        var hasR = MilvusClient.HasCollection(HasCollectionParam.Create(collectionName));
        r.ShouldSuccess();
        hasR.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ShowCollectionsTest()
    {
        this.GivenNoCollection(collectionName);
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var r = MilvusClient.ShowCollections(ShowCollectionsParam.Create(null));

        r.ShouldSuccess();
        r.Data.CollectionNames.Should().Contain(collectionName);
    }

    [Fact]
    public async Task DescribeCollectionTest()
    {
        this.GivenNoCollection(collectionName);
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var r = MilvusClient.DescribeCollection(DescribeCollectionParam.Create(collectionName));

        r.ShouldSuccess();
        r.Data.Schema.Name.Should().Be(collectionName);
    }

    [Fact]
    public async Task DescribeCollectionWithNameTest()
    {
        this.GivenNoCollection(collectionName);
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var r = MilvusClient.DescribeCollection(collectionName);

        r.ShouldSuccess();
        r.Data.Schema.Name.Should().Be(collectionName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetCollectionStatisticsTest(bool isFlushCollection)
    {
        this.GivenNoCollection(collectionName);
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var r = MilvusClient.GetCollectionStatistics(
            GetCollectionStatisticsParam.Create(collectionName, isFlushCollection));

        r.ShouldSuccess();
        r.Data.Stats.First().Key.Should().Be("row_count");
    }

    [Fact]
    public async Task LoadCollectionTest()
    {
        this.GivenNoCollection(collectionName);
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var r = MilvusClient.LoadCollection(LoadCollectionParam.Create(collectionName));

        r.ShouldSuccess();
        r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);

        r = MilvusClient.ReleaseCollection(ReleaseCollectionParam.Create(collectionName));

        r.ShouldSuccess();
        r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);
    }

    [Fact]
    public async Task LoadCollectionWithNameTest()
    {
        this.GivenNoCollection(collectionName);
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var r = MilvusClient.LoadCollection(collectionName);

        r.ShouldSuccess();
        r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);

        r = MilvusClient.ReleaseCollection(collectionName);

        r.ShouldSuccess();
        r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);
    }

    [Fact]
    public async Task LoadCollectionAsyncTestAsync()
    {
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var r = await MilvusClient.LoadCollectionAsync(collectionName);
        r.ShouldSuccess();
        r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);

        r = await MilvusClient.ReleaseCollectionAsync(collectionName);

        r.ShouldSuccess();
        r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);
    }

    [Fact]
    public async Task LoadCollectionAsyncWithNameTestAsync()
    {
        await this.GivenCollection(collectionName);
        this.GivenBookIndex(collectionName);

        var r = await MilvusClient.LoadCollectionAsync(collectionName);

        r.ShouldSuccess();
        r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);

        r = await MilvusClient.ReleaseCollectionAsync(collectionName);

        r.ShouldSuccess();
        r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);
    }
}