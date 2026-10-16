using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

const int DIMENSION = 4;

string uri = Environment.GetEnvironmentVariable("MILVUS_URI") ?? "localhost:19530";
string token = Environment.GetEnvironmentVariable("MILVUS_TOKEN") ?? "root:Milvus";
string collection = TutorialCollectionName();

// MilvusClientV2 connects to the Milvus endpoint and authenticates all following DML requests.
Console.WriteLine("Calling MilvusClientV2: connect to Milvus");
using var client = CreateClient(uri, token);
await client.ConnectAsync();
Console.WriteLine("MilvusClientV2 connected");

// Capture the tutorial result so cleanup still runs if a later operation fails.
Exception? tutorialError = await DemonstrateDml(client, collection);
Exception? cleanupError = await CleanupCollection(client, collection);

if (cleanupError is not null)
{
    Console.WriteLine($"Failed to clean up {collection}: {cleanupError.Message}");
}

if (tutorialError is not null)
{
    throw tutorialError;
}

if (cleanupError is not null)
{
    throw cleanupError;
}

async Task<Exception?> DemonstrateDml(MilvusClientV2 client, string collection)
{
    try
    {
        await CreateAndLoadCollection(client, collection);

        // InsertAsync writes row-oriented entities. `RowsData` holds dictionaries whose keys name the
        // schema fields and whose values follow the declared types.
        Console.WriteLine("Calling InsertAsync: write two row-oriented entities");
        MutationResp inserted = await client.InsertAsync(new InsertReq
        {
            CollectionName = collection,
            RowsData =
            [
                new Dictionary<string, object?> { ["id"] = 1L, ["title"] = "C# in Action", ["price"] = 35.0f, ["embedding"] = new[] { 0.1f, 0.2f, 0.3f, 0.4f } },
                new Dictionary<string, object?> { ["id"] = 2L, ["title"] = "Vector Search", ["price"] = 25.0f, ["embedding"] = new[] { 0.2f, 0.3f, 0.4f, 0.5f } }
            ]
        });
        Console.WriteLine($"InsertAsync completed: inserted {inserted.InsertCount} rows with row input");

        // InsertAsync also accepts column-oriented `ColumnsData`. Every entry names a schema field and
        // all columns must contain the same non-zero number of values.
        Console.WriteLine("Calling InsertAsync: write two column-oriented entities");
        inserted = await client.InsertAsync(new InsertReq
        {
            CollectionName = collection,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 3, 4 }),
                FieldData.CreateVarChar("title", new[] { "Milvus Guide", "Database Systems" }),
                FieldData.Create("price", new[] { 30.0f, 40.0f }),
                FieldData.CreateFloatVector("embedding", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 0.3f, 0.4f, 0.5f, 0.6f }),
                    new ReadOnlyMemory<float>(new[] { 0.4f, 0.5f, 0.6f, 0.7f })
                })
            ]
        });
        Console.WriteLine($"InsertAsync completed: inserted {inserted.InsertCount} rows with column input");

        // UpsertAsync replaces an existing entity or inserts it when the primary key is absent.
        Console.WriteLine("Calling UpsertAsync: fully replace entity id=2");
        MutationResp upserted = await client.UpsertAsync(new UpsertReq
        {
            CollectionName = collection,
            RowsData =
            [
                new Dictionary<string, object?> { ["id"] = 2L, ["title"] = "Practical Vector Search", ["price"] = 27.5f, ["embedding"] = new[] { 0.25f, 0.35f, 0.45f, 0.55f } }
            ]
        });
        Console.WriteLine($"UpsertAsync completed: fully upserted {upserted.UpsertCount} row");

        // UpsertAsync with `PartialUpdate = true` changes only the supplied non-primary fields; the
        // primary key still identifies which entity to update.
        Console.WriteLine("Calling UpsertAsync: partially update entity id=3");
        upserted = await client.UpsertAsync(new UpsertReq
        {
            CollectionName = collection,
            RowsData =
            [
                new Dictionary<string, object?> { ["id"] = 3L, ["title"] = "The Milvus Guide" }
            ],
            PartialUpdate = true
        });
        Console.WriteLine($"UpsertAsync completed: partially upserted {upserted.UpsertCount} row");

        // DeleteAsync with `Ids` removes entities by primary key.
        Console.WriteLine("Calling DeleteAsync: remove entity id=1");
        MutationResp deleted = await client.DeleteAsync(new DeleteReq
        {
            CollectionName = collection,
            Ids = new object[] { 1L }
        });
        Console.WriteLine($"DeleteAsync completed: deleted {deleted.DeleteCount} row by primary key");

        // DeleteAsync with `Expression` removes all entities matching the Milvus boolean expression.
        Console.WriteLine("Calling DeleteAsync: remove entities matching id >= 4");
        deleted = await client.DeleteAsync(new DeleteReq
        {
            CollectionName = collection,
            Expression = "id >= 4"
        });
        Console.WriteLine($"DeleteAsync completed: deleted {deleted.DeleteCount} row by filter");

        await PrintRemainingRows(client, collection);
        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

async Task CreateAndLoadCollection(MilvusClientV2 client, string collection)
{
    var schema = new CollectionSchema
    {
        EnableDynamicFields = false,
        Fields =
        {
            new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
            FieldSchema.CreateVarchar("title", maxLength: 256),
            new FieldSchema("price", DataType.Float),
            FieldSchema.CreateFloatVector("embedding", dimension: DIMENSION)
        }
    };

    Console.WriteLine($"Calling CreateCollectionAsync: create {collection}");
    await client.CreateCollectionAsync(new CreateCollectionReq
    {
        CollectionName = collection,
        Schema = schema,
        Indexes =
        [
            new IndexParam("embedding", indexType: IndexType.AutoIndex, metricType: SimilarityMetricType.Cosine)
        ]
    });
    Console.WriteLine("CreateCollectionAsync completed");

    Console.WriteLine($"Calling LoadCollectionAsync: load {collection}");
    await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collection, Sync = true, TimeoutMs = 60_000 });
    Console.WriteLine("LoadCollectionAsync completed");
}

async Task PrintRemainingRows(MilvusClientV2 client, string collection)
{
    Console.WriteLine("Calling QueryAsync: verify remaining rows");
    QueryResp response = await client.QueryAsync(new QueryReq
    {
        CollectionName = collection,
        Expression = "id >= 0",
        Parameters = new QueryParameters
        {
            OutputFields = { "id", "title", "price" },
            ConsistencyLevel = ConsistencyLevel.Strong
        }
    });
    Console.WriteLine("QueryAsync completed");
    Console.WriteLine("\nRemaining rows:");
    for (int i = 0; i < response.RowCount; i++)
    {
        IReadOnlyDictionary<string, object?> row = response.GetRow(i);
        Console.WriteLine($"  id={row["id"]}, title={row["title"]}, price={row["price"]}");
    }
}

async Task<Exception?> CleanupCollection(MilvusClientV2 client, string collection)
{
    try
    {
        var exists = (await client.HasCollectionAsync(new HasCollectionReq { CollectionName = collection })).Has;
        if (exists)
        {
            Console.WriteLine("Calling ReleaseCollectionAsync: cleanup");
            try
            {
                await client.ReleaseCollectionAsync(new ReleaseCollectionReq { CollectionName = collection });
                Console.WriteLine("ReleaseCollectionAsync completed: ok");
            }
            catch (MilvusException ex)
            {
                Console.WriteLine($"ReleaseCollectionAsync completed: ignored error ({ex.Message})");
            }

            Console.WriteLine($"Calling DropCollectionAsync: remove {collection}");
            await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collection });
            Console.WriteLine("DropCollectionAsync completed");
        }

        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

static MilvusClientV2 CreateClient(string uri, string token)
{
    var config = new ConnectConfig { Uri = uri };
    int colon = token.IndexOf(':');
    if (colon > 0)
    {
        config.Username = token[..colon];
        config.Password = token[(colon + 1)..];
    }
    else
    {
        config.ApiKey = token;
    }

    return new MilvusClientV2(config);
}

static string TutorialCollectionName()
{
    return $"CSHARP_V2_DML_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Environment.ProcessId}";
}
