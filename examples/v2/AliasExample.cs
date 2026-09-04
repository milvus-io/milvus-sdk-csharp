using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Aliases;
using Milvus.Client.V2.Responses.Aliases;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates collection aliases.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show the full alias lifecycle: point an alias at a collection, describe and
/// list it, re-point it at another collection (<c>AlterAliasAsync</c>), and drop it.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateAliasAsync</c>,
/// <c>DescribeAliasAsync</c>, <c>AlterAliasAsync</c>, <c>ListAliasesAsync</c>,
/// <c>DropAliasAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Described alias -> coll_one", "Aliases: alias_example_alias",
/// "After alter, alias points to coll_two", then "Done.".</para>
/// </remarks>
public static class AliasExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "alias_example_coll_one";
        const string secondCollectionName = "alias_example_coll_two";
        const string alias = "alias_example_alias";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);
        await ExampleHelpers.ResetCollectionAsync(client, secondCollectionName);

        #region Snippet:MilvusAlias_Create
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });

        // The alias points at coll_one initially.
        await client.CreateAliasAsync(new CreateAliasReq { CollectionName = collectionName, Alias = alias });

        DescribeAliasResp described = await client.DescribeAliasAsync(new DescribeAliasReq { Alias = alias });
        Console.WriteLine($"Described alias -> {described.CollectionName}");
        #endregion

        ListAliasesResp aliases = await client.ListAliasesAsync(new ListAliasesReq { CollectionName = collectionName });
        Console.WriteLine($"Aliases: {string.Join(", ", aliases.Aliases)}");

        #region Snippet:MilvusAlias_Alter
        // Re-point the alias at coll_two.
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = secondCollectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });
        await client.AlterAliasAsync(new AlterAliasReq { CollectionName = secondCollectionName, Alias = alias });

        described = await client.DescribeAliasAsync(new DescribeAliasReq { Alias = alias });
        Console.WriteLine($"After alter, alias points to {described.CollectionName}");
        #endregion

        await client.DropAliasAsync(new DropAliasReq { Alias = alias });
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = secondCollectionName });

        Console.WriteLine("Done.");
    }
}
