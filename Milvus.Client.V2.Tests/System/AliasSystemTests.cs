using Xunit;

using Milvus.Client.V2.Requests.Aliases;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Responses.Aliases;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests;

[Trait("Category", "System")]
[Collection(MilvusV2Collection.Name)]
public class AliasSystemTests : SystemTestBase
{
    public AliasSystemTests(MilvusV2Fixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Create_Describe_Drop_Alias_round_trip()
    {
        const string collectionName = nameof(Create_Describe_Drop_Alias_round_trip);
        const string aliasName = "my_alias";

        await ResetCollectionAsync(collectionName);
        await ResetAliasAsync(aliasName);

        await Client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("dummy_vec", dimension: 2)
                }
            }
        }, TestContext.Current.CancellationToken);

        await Client.CreateAliasAsync(new CreateAliasReq { CollectionName = collectionName, Alias = aliasName },
            TestContext.Current.CancellationToken);

        DescribeAliasResp describe = await Client.DescribeAliasAsync(new DescribeAliasReq { Alias = aliasName },
            TestContext.Current.CancellationToken);
        Assert.Equal(aliasName, describe.Alias);

        // Alter the alias to point elsewhere (no-op here but exercises the wire path).
        await Client.AlterAliasAsync(new AlterAliasReq { CollectionName = collectionName, Alias = aliasName },
            TestContext.Current.CancellationToken);

        await Client.DropAliasAsync(new DropAliasReq { Alias = aliasName },
            TestContext.Current.CancellationToken);

        await ResetCollectionAsync(collectionName);
    }

    private async Task ResetAliasAsync(string alias)
    {
        // Drop the alias unconditionally; DescribeAlias on a missing alias throws.
        try
        {
            await Client.DescribeAliasAsync(new DescribeAliasReq { Alias = alias }, TestContext.Current.CancellationToken);
            await Client.DropAliasAsync(new DropAliasReq { Alias = alias }, TestContext.Current.CancellationToken);
        }
        catch (MilvusException)
        {
            // Alias did not exist; nothing to clean up.
        }
    }
}
