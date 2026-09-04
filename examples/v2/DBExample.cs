using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Database;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Database;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates databases: create/list/describe/drop, switching the default database with
/// <c>UseDatabaseAsync</c>, and targeting a non-default database per request via
/// <c>DatabaseName</c> (request-level override).
/// Mirrors cpp examples/src/v2/db.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show database lifecycle and both ways to scope an operation to a
/// database: client-level (<c>UseDatabaseAsync</c>) and request-level (<c>DatabaseName</c>).</para>
/// <para><b>APIs used:</b> <c>CreateDatabaseAsync</c>, <c>ListDatabasesAsync</c>,
/// <c>DescribeDatabaseAsync</c>, <c>CreateCollectionAsync</c> (with <c>DatabaseName</c>),
/// <c>UseDatabaseAsync</c>, <c>DropDatabaseAsync</c>.</para>
/// <para><b>Expected output:</b> the database list, then "Done.".</para>
/// </remarks>
public static class DBExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string dbName = "example_db";
        const string collectionName = "db_example_coll";

        // Drop the database if a previous run left it behind (drops cascade to its collections).
        try
        {
            await client.DropDatabaseAsync(new DropDatabaseReq { DatabaseName = dbName });
        }
        catch (MilvusException)
        {
            // The database may not exist yet.
        }

        #region Snippet:MilvusDb_Create
        await client.CreateDatabaseAsync(new CreateDatabaseReq { DatabaseName = dbName });

        ListDatabasesResp databases = await client.ListDatabasesAsync(new ListDatabasesReq());
        Console.WriteLine($"Databases: {string.Join(", ", databases.DatabaseNames)}");

        DescribeDatabaseResp described = await client.DescribeDatabaseAsync(new DescribeDatabaseReq { DatabaseName = dbName });
        Console.WriteLine($"Described database: {described.DatabaseName}");
        #endregion

        // Option 1: switch the client's default database.
        await client.UseDatabaseAsync(new UseDatabaseReq { DatabaseName = dbName });
        Console.WriteLine($"Switched to database: {dbName}");

        // Option 2: target a non-default database per request (request-level override).
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            DatabaseName = dbName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: 4)
                }
            }
        });
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName, DatabaseName = dbName });

        await client.DropDatabaseAsync(new DropDatabaseReq { DatabaseName = dbName });
        Console.WriteLine("Done.");
    }
}
