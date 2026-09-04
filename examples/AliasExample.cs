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
/// <para><b>Purpose:</b> show how to point an alias at a collection so callers can address it by a
/// stable name.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateAliasAsync</c>,
/// <c>ListAliasesAsync</c>, <c>DropAliasAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Aliases: alias_example_alias", then "Done.".</para>
/// </remarks>
public static class AliasExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "alias_example";
        const string alias = "alias_example_alias";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

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

        await client.CreateAliasAsync(new CreateAliasReq { CollectionName = collectionName, Alias = alias });
        #endregion

        ListAliasesResp aliases = await client.ListAliasesAsync(new ListAliasesReq { CollectionName = collectionName });
        Console.WriteLine($"Aliases: {string.Join(", ", aliases.Aliases)}");

        await client.DropAliasAsync(new DropAliasReq { Alias = alias });
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });

        Console.WriteLine("Done.");
    }
}
