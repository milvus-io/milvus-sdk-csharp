using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;

const string TTL_SECONDS = "collection.ttl.seconds";
const string ID_FIELD = "id";
const string TITLE_FIELD = "title";
const string VECTOR_FIELD = "embedding";

string uri = Environment.GetEnvironmentVariable("MILVUS_URI") ?? "localhost:19530";
string token = Environment.GetEnvironmentVariable("MILVUS_TOKEN") ?? "root:Milvus";
string collection = TutorialCollectionName();
string renamedCollection = $"{collection}_renamed";

// MilvusClientV2 connects to the endpoint on first use; the same client is reused for the complete
// collection lifecycle.
Console.WriteLine("Calling MilvusClientV2: connect to Milvus");
using var client = CreateClient(uri, token);
await client.ConnectAsync();
Console.WriteLine("MilvusClientV2 connected");

await PrintCollections(client, "Collections before the tutorial");

// Capture the tutorial result so cleanup still runs if a later operation fails.
Exception? tutorialError = await DemonstrateCollectionInterfaces(client, collection, renamedCollection);
Exception? cleanupError = await CleanupCollections(client, [collection, renamedCollection]);

if (cleanupError is not null)
{
    Console.WriteLine($"Failed to clean up the tutorial collection: {cleanupError.Message}");
}

if (tutorialError is not null)
{
    throw tutorialError;
}

if (cleanupError is not null)
{
    throw cleanupError;
}

await PrintCollections(client, "Collections after cleanup");

async Task<Exception?> DemonstrateCollectionInterfaces(MilvusClientV2 client, string collection, string renamedCollection)
{
    try
    {
        var schema = new CollectionSchema
        {
            Description = "Collection tutorial schema",
            EnableDynamicFields = false,
            Fields =
            {
                new FieldSchema(ID_FIELD, DataType.Int64, isPrimaryKey: true),
                FieldSchema.CreateVarchar(TITLE_FIELD, maxLength: 256),
                FieldSchema.CreateFloatVector(VECTOR_FIELD, dimension: 4)
            }
        };

        Console.WriteLine($"\nCreating collection {collection}");
        Console.WriteLine("Calling CreateCollectionAsync: create with description, vector index and Bounded consistency");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collection,
            Schema = schema,
            ConsistencyLevel = ConsistencyLevel.BoundedStaleness,
            Indexes =
            [
                new IndexParam(VECTOR_FIELD, indexType: IndexType.AutoIndex, metricType: MetricType.Cosine)
            ]
        });
        Console.WriteLine("CreateCollectionAsync completed");

        Console.WriteLine("Calling HasCollectionAsync: check existence");
        HasCollectionResp exists = await client.HasCollectionAsync(new HasCollectionReq { CollectionName = collection });
        Console.WriteLine($"HasCollectionAsync completed: exists={exists.Has}");

        await DescribeCollection(client, collection);
        await DescribeProperty(client, collection);

        Console.WriteLine($"\nSetting {TTL_SECONDS}=3600");
        Console.WriteLine("Calling AlterCollectionPropertiesAsync: set TTL");
        await client.AlterCollectionPropertiesAsync(new AlterCollectionPropertiesReq
        {
            CollectionName = collection,
            Properties = { [TTL_SECONDS] = "3600" }
        });
        Console.WriteLine("AlterCollectionPropertiesAsync completed");
        await DescribeProperty(client, collection);

        Console.WriteLine($"Removing {TTL_SECONDS}");
        Console.WriteLine("Calling DropCollectionPropertiesAsync: remove TTL");
        await client.DropCollectionPropertiesAsync(new DropCollectionPropertiesReq
        {
            CollectionName = collection,
            DeleteKeys = [TTL_SECONDS]
        });
        Console.WriteLine("DropCollectionPropertiesAsync completed");
        await DescribeProperty(client, collection);

        Console.WriteLine("\nLoading the collection");
        Console.WriteLine("Calling LoadCollectionAsync: load synchronously");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collection, Sync = true, TimeoutMs = 60_000 });
        Console.WriteLine("LoadCollectionAsync completed");

        Console.WriteLine("Calling GetLoadStateAsync: inspect load state");
        GetLoadStateResp loadState = await client.GetLoadStateAsync(new GetLoadStateReq { CollectionName = collection });
        Console.WriteLine($"GetLoadStateAsync completed: state={loadState.State}, progress={loadState.Progress}%");

        Console.WriteLine("Calling GetCollectionStatsAsync: inspect row count");
        GetCollectionStatsResp stats = await client.GetCollectionStatsAsync(new GetCollectionStatsReq { CollectionName = collection });
        Console.WriteLine($"GetCollectionStatsAsync completed: row_count={stats.RowCount}");

        Console.WriteLine("\nReleasing the collection");
        Console.WriteLine("Calling ReleaseCollectionAsync: release from serving memory");
        await client.ReleaseCollectionAsync(new ReleaseCollectionReq { CollectionName = collection });
        Console.WriteLine("ReleaseCollectionAsync completed");

        Console.WriteLine($"Renaming {collection} to {renamedCollection}");
        Console.WriteLine("Calling RenameCollectionAsync");
        await client.RenameCollectionAsync(new RenameCollectionReq
        {
            CollectionName = collection,
            NewCollectionName = renamedCollection
        });
        Console.WriteLine("RenameCollectionAsync completed");

        bool oldExists = (await client.HasCollectionAsync(new HasCollectionReq { CollectionName = collection })).Has;
        bool newExists = (await client.HasCollectionAsync(new HasCollectionReq { CollectionName = renamedCollection })).Has;
        Console.WriteLine($"After rename: old name exists={oldExists}, new name exists={newExists}");

        Console.WriteLine($"Truncating {renamedCollection}");
        Console.WriteLine("Calling TruncateCollectionAsync: remove all entities, keep schema and indexes");
        await client.TruncateCollectionAsync(new TruncateCollectionReq { CollectionName = renamedCollection });
        Console.WriteLine("TruncateCollectionAsync completed");
        Console.WriteLine("Truncate removes all entities but preserves the collection schema and indexes.");

        await PrintCollections(client, "Collections before cleanup");
        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

async Task<Exception?> CleanupCollections(MilvusClientV2 client, IReadOnlyList<string> collections)
{
    Exception? firstError = null;
    foreach (string name in collections)
    {
        try
        {
            HasCollectionResp exists = await client.HasCollectionAsync(new HasCollectionReq { CollectionName = name });
            if (!exists.Has)
            {
                continue;
            }

            Console.WriteLine($"Calling ReleaseCollectionAsync: cleanup {name}");
            try
            {
                await client.ReleaseCollectionAsync(new ReleaseCollectionReq { CollectionName = name });
                Console.WriteLine("ReleaseCollectionAsync completed: ok");
            }
            catch (MilvusException ex)
            {
                Console.WriteLine($"ReleaseCollectionAsync completed: ignored error ({ex.Message})");
            }

            Console.WriteLine($"Calling DropCollectionAsync: remove {name}");
            await client.DropCollectionAsync(new DropCollectionReq { CollectionName = name });
            Console.WriteLine("DropCollectionAsync completed");
        }
        catch (Exception ex)
        {
            firstError ??= ex;
        }
    }

    return firstError;
}

async Task DescribeCollection(MilvusClientV2 client, string collection)
{
    Console.WriteLine("Calling DescribeCollectionAsync: inspect schema and metadata");
    DescribeCollectionResp response = await client.DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = collection });
    Console.WriteLine($"DescribeCollectionAsync completed: name={response.CollectionName}, id={response.CollectionId}, " +
        $"primary_field={response.Schema.Fields.First(f => f.IsPrimaryKey).Name}, " +
        $"vector_fields={string.Join(",", response.Schema.Fields.Where(f => f.DataType == DataType.FloatVector).Select(f => f.Name))}");
}

async Task DescribeProperty(MilvusClientV2 client, string collection)
{
    Console.WriteLine("Calling DescribeCollectionAsync: inspect properties");
    DescribeCollectionResp response = await client.DescribeCollectionAsync(new DescribeCollectionReq { CollectionName = collection });
    Console.WriteLine($"Property value: {response.Properties.GetValueOrDefault(TTL_SECONDS, "<not set>")}");
}

async Task PrintCollections(MilvusClientV2 client, string heading)
{
    Console.WriteLine("Calling ListCollectionsAsync");
    ListCollectionsResp response = await client.ListCollectionsAsync(new ListCollectionsReq());
    Console.WriteLine($"ListCollectionsAsync completed - {heading}: {string.Join(", ", response.CollectionNames)}");
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
    return $"CSHARP_V2_COLLECTION_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Environment.ProcessId}";
}
