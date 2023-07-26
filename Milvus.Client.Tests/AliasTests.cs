using Xunit;

namespace Milvus.Client.Tests;

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

    public string CollectionName = nameof(AliasTests);

    private MilvusCollection Collection { get; set; }

    public AliasTests()
        => Collection = Client.GetCollection(CollectionName);

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

    private MilvusClient Client => TestEnvironment.Client;

    public Task DisposeAsync()
        => Task.CompletedTask;
}
