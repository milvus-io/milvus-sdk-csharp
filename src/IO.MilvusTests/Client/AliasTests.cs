using IO.Milvus.Param.Alias;
using IO.MilvusTests.Client.Base;
using IO.MilvusTests.Helpers;
using Xunit;

namespace IO.MilvusTests.Client;

/// <summary>
/// unit test about alias
/// the tests must be executed in order of alphabet. A -> B -> C
///
/// </summary>
/// <remarks>
/// <see cref="https://milvus.io/docs/v2.0.x/collection_alias.md"/>
/// </remarks>
public sealed class AliasTests : MilvusServiceClientTestsBase, IAsyncLifetime
{
    private string collectionName;
    private string aliasName;

    public async Task InitializeAsync()
    {
        collectionName = $"test{random.Next()}";

        aliasName = collectionName + "_aliasName";

        await CreateBookCollectionAsync(collectionName);
    }

    public async Task DisposeAsync()
    {
        DropAlias(collectionName, aliasName);

        this.ThenDropCollection(collectionName);

        // Cooldown, sometimes the DB doesn't refresh completely
        await Task.Delay(1000);
    }

    [Fact]
    public void ACreateAliasTest()
    {
        var r = CreateAlias(collectionName, aliasName);

        r.AssertRpcStatus();
    }

    [Fact]
    public void BAlterAliasTest()
    {
        CreateAlias(collectionName, aliasName);

        //TODO: alter to another collection,not self.
        var param = AlterAliasParam.Create(collectionName, aliasName);
        var r = MilvusClient.AlterAlias(param);
        r.AssertRpcStatus();
    }

    [Fact]
    public void CDropAliasTest()
    {
        MilvusClient.CreateAlias(CreateAliasParam.Create(collectionName, aliasName));

        var r = DropAlias(collectionName, aliasName);
        r.AssertRpcStatus();
    }
}