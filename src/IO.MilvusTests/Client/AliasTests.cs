using IO.Milvus;
using IO.Milvus.Client;
using IO.Milvus.Client.gRPC;
using Xunit;

namespace IO.MilvusTests.Client;

public class AliasTests : IAsyncLifetime
{
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Create(IMilvusClient client)
    {
        await client.DropAliasAsync("a");
        await client.DropAliasAsync("b");

        await client.CreateAliasAsync(CollectionName, "a");
        await client.CreateAliasAsync(CollectionName, "b");

        var description = await client.DescribeCollectionAsync(CollectionName);
        Assert.Collection(description.Aliases.Order(),
            alias => Assert.Equal("a", alias),
            alias => Assert.Equal("b", alias));
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Alter(IMilvusClient client)
    {
        await client.DropAliasAsync("a");
        await client.CreateAliasAsync(CollectionName, "a");

        await client.DropCollectionAsync("AnotherCollection");
        await client.CreateCollectionAsync(
            "AnotherCollection",
            new[] { FieldType.Create<long>("id", isPrimaryKey: true) });

        await client.AlterAliasAsync("AnotherCollection", "a");

        var description1 = await client.DescribeCollectionAsync(CollectionName);
        Assert.DoesNotContain(description1.Aliases, alias => alias == "a");

        var description2 = await client.DescribeCollectionAsync("AnotherCollection");
        Assert.Collection(description2.Aliases, alias => Assert.Equal("a", alias));
    }

    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task Drop(IMilvusClient client)
    {
        await client.DropAliasAsync("a");
        await client.CreateAliasAsync(CollectionName, "a");

        await client.DropAliasAsync("a");

        var description = await client.DescribeCollectionAsync(CollectionName);
        Assert.DoesNotContain(description.Aliases, alias => alias == "a");
    }

    public string CollectionName => nameof(AliasTests);

    public async Task InitializeAsync()
    {
        var config = MilvusConfig.Load().FirstOrDefault();
        if (config is null)
        {
            throw new InvalidOperationException("No client configs");
        }

        var client = config.CreateClient();

        await client.DropCollectionAsync(CollectionName);
        await client.CreateCollectionAsync(
            CollectionName,
            new[]
            {
                FieldType.Create<long>("id", isPrimaryKey: true),
                FieldType.CreateVarchar("varchar", 256),
                FieldType.CreateFloatVector("float_vector", 2)
            });
    }

    public Task DisposeAsync()
        => Task.CompletedTask;
}
