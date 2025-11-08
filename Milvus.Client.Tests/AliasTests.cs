using Xunit;

namespace Milvus.Client.Tests;

[Collection("Milvus")]
public class AliasTests : IAsyncLifetime
{
    [Fact]
    public async Task Create()
    {
        await Client.CreateAliasAsync(CollectionName, "a");
        await Client.CreateAliasAsync(CollectionName, "b");

        var description = await Collection.DescribeAsync();
        Assert.Collection(description.Aliases.Order(),
            alias => Assert.Equal("a", alias),
            alias => Assert.Equal("b", alias));
    }

    // [Fact]
    // public async Task Alter()
    // {
    //     await Client.CreateAliasAsync(CollectionName, "a");
    //
    //     await Client.DropCollectionAsync("AnotherCollection");
    //     await Client.CreateCollectionAsync(
    //         "AnotherCollection",
    //         new[] { FieldSchema.Create<long>("id", isPrimaryKey: true) });
    //
    //     await Client.AlterAliasAsync("AnotherCollection", "a");
    //
    //     var description1 = await Client.DescribeCollectionAsync(CollectionName);
    //     Assert.DoesNotContain(description1.Aliases, alias => alias == "a");
    //
    //     var description2 = await Client.DescribeCollectionAsync("AnotherCollection");
    //     Assert.Collection(description2.Aliases, alias => Assert.Equal("a", alias));
    // }

    [Fact]
    public async Task Drop()
    {
        await Client.CreateAliasAsync(CollectionName, "a");

        await Client.DropAliasAsync("a");

        var description = await Collection.DescribeAsync();
        Assert.DoesNotContain(description.Aliases, alias => alias == "a");
    }

    [Fact]
    public async Task DescribeAlias()
    {
        await Client.CreateAliasAsync(CollectionName, "test_alias");

        string collectionName = await Client.DescribeAliasAsync("test_alias");

        Assert.Equal(CollectionName, collectionName);
    }

    [Fact]
    public async Task DescribeAlias_unknown()
    {
        var exception = await Assert.ThrowsAsync<MilvusException>(() => Client.DescribeAliasAsync("unknown_alias"));

        Assert.Contains("alias not found", exception.Message);
    }

    [Fact]
    public async Task ListAliases()
    {
        await Client.CreateAliasAsync(CollectionName, "alias1");
        await Client.CreateAliasAsync(CollectionName, "alias2");

        IList<string> aliases = await Client.ListAliasesAsync();

        Assert.Contains("alias1", aliases);
        Assert.Contains("alias2", aliases);
    }

    [Fact]
    public async Task ListAliases_FilterByCollection()
    {
        await Client.CreateAliasAsync(CollectionName, "alias_for_collection");

        IList<string> aliases = await Client.ListAliasesAsync(CollectionName);

        Assert.Contains("alias_for_collection", aliases);
    }

    public string CollectionName = nameof(AliasTests);

    private MilvusCollection Collection { get; set; }

    public AliasTests(MilvusFixture milvusFixture)
    {
        Client = milvusFixture.CreateClient();
        Collection = Client.GetCollection(CollectionName);
    }

    public async Task InitializeAsync()
    {
        await Client.DropAliasAsync("a");
        await Client.DropAliasAsync("b");

        await Collection.DropAsync();
        Collection = await Client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldSchema.Create<long>("id", isPrimaryKey: true),
                FieldSchema.CreateVarchar("varchar", 256),
                FieldSchema.CreateFloatVector("float_vector", 2)
            });
    }

    private readonly MilvusClient Client;

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
