using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

/// <summary>
/// unit test about alias
/// the tests must be executed in order of alphabet. A -> B -> C
/// </summary>
/// <remarks>
/// <see cref="https://milvus.io/docs/v2.0.x/collection_alias.md"/>
/// </remarks>
public sealed class AliasTests : MilvusTestClientsBase, IAsyncLifetime
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    private string _collectionName;
    private string _aliasName;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

    public async Task InitializeAsync()
    {
        Random random = new();
        _collectionName = $"test{random.Next()}";

        _aliasName = _collectionName + "_aliasName";

        foreach (var client in MilvusClients)
        {
            if (client.IsZillizCloud())
            {
                continue;
            }
            await client.CreateBookCollectionAsync(_collectionName);
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var client in MilvusClients)
        {
            if (client.IsZillizCloud())
            {
                continue;
            }
            await client.DropAliasAsync(_aliasName);
            await client.DropCollectionAsync(_collectionName);
        }

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Fact]
    public async Task ACreateAliasTest()
    {
        foreach (var client in MilvusClients)
        {
            if (client.IsZillizCloud())
            {
                continue;
            }
            await client.CreateAliasAsync(_collectionName,_aliasName);
        }
    }

    [Fact]
    public async Task BAlterAliasTest()
    {
        //TODO: alter to another collection,not self.
        foreach (var client in MilvusClients)
        {
            if (client.IsZillizCloud())
            {
                continue;
            }
            await client.CreateAliasAsync(_collectionName, _aliasName);
            await client.AlterAliasAsync(_collectionName, _aliasName);
        }
    }

    [Fact]
    public async Task CDropAliasTest()
    {
        foreach (var client in MilvusClients)
        {
            if (client.IsZillizCloud())
            {
                continue;
            }
            await client.DropAliasAsync(_aliasName);
        }
    }
}