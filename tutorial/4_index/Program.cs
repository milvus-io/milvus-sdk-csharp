using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Index;
using Milvus.Client.V2.Types;

const string CATEGORY_INDEX = "category_inverted_idx";
const string PRICE_INDEX = "price_sort_idx";
const string VECTOR_INDEX = "embedding_hnsw_idx";

string uri = Environment.GetEnvironmentVariable("MILVUS_URI") ?? "localhost:19530";
string token = Environment.GetEnvironmentVariable("MILVUS_TOKEN") ?? "root:Milvus";
string collection = TutorialCollectionName();

// MilvusClientV2 connects to `uri` and authenticates with `token` for index administration.
Console.WriteLine("Calling MilvusClientV2: connect to Milvus");
using var client = CreateClient(uri, token);
await client.ConnectAsync();
Console.WriteLine("MilvusClientV2 connected");

// Capture the tutorial result so cleanup still runs if a later operation fails.
Exception? tutorialError = await DemonstrateIndexes(client, collection);
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

async Task<Exception?> DemonstrateIndexes(MilvusClientV2 client, string collection)
{
    try
    {
        var schema = new CollectionSchema
        {
            EnableDynamicFields = false,
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                FieldSchema.CreateVarchar("category", maxLength: 128),
                new FieldSchema("price", DataType.Float),
                FieldSchema.CreateFloatVector("embedding", dimension: 8)
            }
        };

        Console.WriteLine($"Creating collection {collection} without indexes");
        Console.WriteLine("Calling CreateCollectionAsync: create fields only");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collection,
            Schema = schema
        });
        Console.WriteLine("CreateCollectionAsync completed");

        Console.WriteLine("Creating one vector index and two scalar indexes");
        Console.WriteLine("Calling CreateIndexAsync: build vector and scalar indexes");
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collection,
            Indexes =
            [
                new IndexParam("embedding", VECTOR_INDEX, IndexType.Hnsw, SimilarityMetricType.Cosine)
                {
                    ExtraParams =
                    {
                        ["M"] = "16",
                        ["efConstruction"] = "100"
                    }
                },
                new IndexParam("category", CATEGORY_INDEX, IndexType.Inverted, null),
                new IndexParam("price", PRICE_INDEX, IndexType.StlSort, null)
            ],
            Sync = true,
            TimeoutMs = 60_000
        });
        Console.WriteLine("CreateIndexAsync completed");

        await PrintIndexes(client, collection, "Created indexes");

        Console.WriteLine($"Calling DescribeIndexAsync: inspect {VECTOR_INDEX}");
        DescribeIndexResp described = await client.DescribeIndexAsync(new DescribeIndexReq
        {
            CollectionName = collection,
            FieldName = "embedding",
            IndexName = VECTOR_INDEX
        });
        Console.WriteLine("DescribeIndexAsync completed");
        foreach (IndexDesc index in described.Indexes)
        {
            Console.WriteLine($"Vector index detail: name={index.IndexName}, type={index.IndexType}, " +
                $"metric={index.MetricType}, state={index.State}, params={string.Join(",", index.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        }

        Console.WriteLine($"Dropping scalar index {PRICE_INDEX}");
        Console.WriteLine("Calling DropIndexAsync: remove the price index only");
        await client.DropIndexAsync(new DropIndexReq
        {
            CollectionName = collection,
            IndexName = PRICE_INDEX
        });
        Console.WriteLine("DropIndexAsync completed");
        await PrintIndexes(client, collection, "Indexes after dropping price index");

        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

async Task PrintIndexes(MilvusClientV2 client, string collection, string heading)
{
    Console.WriteLine("Calling ListIndexesAsync");
    ListIndexesResp response = await client.ListIndexesAsync(new ListIndexesReq { CollectionName = collection });
    Console.WriteLine("ListIndexesAsync completed");
    Console.WriteLine($"\n{heading}:");
    foreach (IndexDesc index in response.Indexes)
    {
        Console.WriteLine($"  name={index.IndexName,-24} field={index.FieldName,-10} type={index.IndexType} " +
            $"metric={index.MetricType} state={index.State}");
    }
}

async Task<Exception?> CleanupCollection(MilvusClientV2 client, string collection)
{
    try
    {
        HasCollectionResp exists = await client.HasCollectionAsync(new HasCollectionReq { CollectionName = collection });
        if (!exists.Has)
        {
            return null;
        }

        Console.WriteLine($"\nCalling DropCollectionAsync: remove {collection}");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collection });
        Console.WriteLine("DropCollectionAsync completed");
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
    return $"CSHARP_V2_INDEX_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Environment.ProcessId}";
}
