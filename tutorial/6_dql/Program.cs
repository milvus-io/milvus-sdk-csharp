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

// MilvusClientV2 connects to the endpoint and authenticates all query/search operations.
Console.WriteLine("Calling MilvusClientV2: connect to Milvus");
using var client = CreateClient(uri, token);
await client.ConnectAsync();
Console.WriteLine("MilvusClientV2 connected");

// Capture the tutorial result so cleanup still runs if a later operation fails.
Exception? tutorialError = await DemonstrateDql(client, collection);
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

async Task<Exception?> DemonstrateDql(MilvusClientV2 client, string collection)
{
    try
    {
        await CreateLoadAndInsert(client, collection);
        await RunQuery(client, collection);
        await RunSearch(client, collection);
        await RunHybridSearch(client, collection);
        await RunQueryIterator(client, collection);
        await RunSearchIterator(client, collection);
        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

async Task CreateLoadAndInsert(MilvusClientV2 client, string collection)
{
    // CreateCollectionAsync creates the query target, then CreateIndexAsync builds both indexes. Each
    // IndexParam selects its vector field, index implementation, and similarity metric.
    Console.WriteLine("Calling CreateCollectionAsync: create dense and sparse search fields");
    await client.CreateCollectionAsync(new CreateCollectionReq
    {
        CollectionName = collection,
        Schema = new CollectionSchema
        {
            EnableDynamicFields = false,
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                FieldSchema.CreateVarchar("title", maxLength: 256),
                new FieldSchema("category", DataType.Int32),
                FieldSchema.CreateFloatVector("dense", dimension: DIMENSION),
                FieldSchema.CreateSparseFloatVector("sparse")
            }
        }
    });
    Console.WriteLine("CreateCollectionAsync completed");

    Console.WriteLine("Calling CreateIndexAsync: build dense and sparse indexes");
    await client.CreateIndexAsync(new CreateIndexReq
    {
        CollectionName = collection,
        Indexes =
        [
            new IndexParam("dense", "dense_idx", IndexType.AutoIndex, SimilarityMetricType.Cosine),
            new IndexParam("sparse", "sparse_idx", IndexType.SparseInvertedIndex, SimilarityMetricType.Ip)
        ],
        Sync = true,
        TimeoutMs = 60_000
    });
    Console.WriteLine("CreateIndexAsync completed");

    Console.WriteLine($"Calling LoadCollectionAsync: load {collection}");
    await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collection, Sync = true, TimeoutMs = 60_000 });
    Console.WriteLine("LoadCollectionAsync completed");

    var rows = new List<IDictionary<string, object?>>();
    for (long id = 0; id < 12; id++)
    {
        rows.Add(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["title"] = $"document_{id}",
            ["category"] = (int)(id % 3),
            ["dense"] = DenseVector(id),
            ["sparse"] = SparseVector(id)
        });
    }

    Console.WriteLine("Calling InsertAsync: seed the query and search dataset");
    MutationResp response = await client.InsertAsync(new InsertReq { CollectionName = collection, RowsData = rows });
    Console.WriteLine($"InsertAsync completed: inserted {response.InsertCount} tutorial rows");
}

async Task RunQuery(MilvusClientV2 client, string collection)
{
    Console.WriteLine("\nCalling QueryAsync: filter rows where category == 1");
    QueryResp response = await client.QueryAsync(new QueryReq
    {
        CollectionName = collection,
        Expression = "category == 1",
        Parameters = new QueryParameters
        {
            OutputFields = { "id", "title", "category" },
            Limit = 5,
            ConsistencyLevel = ConsistencyLevel.Strong
        }
    });
    Console.WriteLine("QueryAsync completed");
    for (int i = 0; i < response.RowCount; i++)
    {
        IReadOnlyDictionary<string, object?> row = response.GetRow(i);
        Console.WriteLine($"  id={row["id"]}, title={row["title"]}, category={row["category"]}");
    }
}

async Task RunSearch(MilvusClientV2 client, string collection)
{
    Console.WriteLine("\nCalling SearchAsync: find nearest dense vectors");
    SearchResp response = await client.SearchAsync(new SearchReq
    {
        CollectionName = collection,
        VectorFieldName = "dense",
        Vectors = [DenseVector(2)],
        MetricType = SimilarityMetricType.Cosine,
        Limit = 3,
        Parameters = new SearchParameters
        {
            OutputFields = { "id", "title", "category" },
            ConsistencyLevel = ConsistencyLevel.Strong
        }
    });
    Console.WriteLine("SearchAsync completed");
    PrintSearchRows(response);
}

async Task RunHybridSearch(MilvusClientV2 client, string collection)
{
    Console.WriteLine("\nCalling HybridSearchAsync: combine dense and sparse searches");
    SearchResp response = await client.HybridSearchAsync(new HybridSearchReq
    {
        CollectionName = collection,
        SearchRequests =
        [
            new AnnSearchReq
            {
                VectorFieldName = "dense",
                Vectors = [DenseVector(2)],
                MetricType = SimilarityMetricType.Cosine,
                Limit = 6
            },
            new AnnSearchReq
            {
                VectorFieldName = "sparse",
                SparseVectors = [SparseVector(2)],
                MetricType = SimilarityMetricType.Ip,
                Limit = 6
            }
        ],
        Reranker = new WeightedReranker(0.7f, 0.3f),
        Limit = 4,
        Parameters = new SearchParameters
        {
            OutputFields = { "id", "title", "category" },
            ConsistencyLevel = ConsistencyLevel.Strong
        }
    });
    Console.WriteLine("HybridSearchAsync completed");
    PrintSearchRows(response);
}

async Task RunQueryIterator(MilvusClientV2 client, string collection)
{
    Console.WriteLine("\nCalling QueryIteratorAsync: batch_size=3, limit=7");
    int pageNumber = 0;
    await foreach (IReadOnlyList<FieldData> page in client.QueryIteratorAsync(new QueryIteratorReq
    {
        CollectionName = collection,
        Expression = "id >= 0",
        BatchSize = 3,
        Parameters = new QueryParameters
        {
            OutputFields = { "id", "title", "category" },
            Limit = 7,
            ConsistencyLevel = ConsistencyLevel.Strong
        }
    }))
    {
        pageNumber++;
        Console.WriteLine($"  page {pageNumber}");
        int rows = page.FirstOrDefault()?.RowCount ?? 0;
        for (int i = 0; i < rows; i++)
        {
            var parts = new List<string>();
            foreach (FieldData field in page)
            {
                parts.Add($"{field.FieldName}={GetFieldValue(field, i)}");
            }

            Console.WriteLine($"    {string.Join(", ", parts)}");
        }
    }
}

async Task RunSearchIterator(MilvusClientV2 client, string collection)
{
    Console.WriteLine("\nCalling SearchIteratorAsync: batch_size=3, limit=7");
    int pageNumber = 0;
    await foreach (SingleResult page in client.SearchIteratorAsync(new SearchIteratorReq
    {
        CollectionName = collection,
        VectorFieldName = "dense",
        Vectors = [DenseVector(2)],
        MetricType = SimilarityMetricType.Cosine,
        BatchSize = 3,
        Limit = 7,
        Parameters = new SearchParameters
        {
            OutputFields = { "id", "title", "category" },
            ConsistencyLevel = ConsistencyLevel.Strong
        }
    }))
    {
        pageNumber++;
        Console.WriteLine($"  page {pageNumber}");
        for (int i = 0; i < page.Scores.Count; i++)
        {
            IReadOnlyDictionary<string, object?> row = page.GetRow(i);
            Console.WriteLine($"    id={row["id"]}, score={page.Scores[i]:F4}, title={row["title"]}, category={row["category"]}");
        }
    }
}

void PrintSearchRows(SearchResp response)
{
    SingleResult? single = response.SingleResults?.FirstOrDefault();
    if (single is null)
    {
        return;
    }

    for (int i = 0; i < single.Scores.Count; i++)
    {
        IReadOnlyDictionary<string, object?> row = single.GetRow(i);
        Console.WriteLine($"  id={row["id"]}, score={single.Scores[i]:F4}, title={row["title"]}, category={row["category"]}");
    }
}

static object? GetFieldValue(FieldData field, int index)
    => field switch
    {
        FieldData<long> longs => longs.Data[index],
        FieldData<long?> longs => longs.Data[index],
        FieldData<int> ints => ints.Data[index],
        FieldData<int?> ints => ints.Data[index],
        FieldData<string> strings => strings.Data[index],
        FieldData<float> floats => floats.Data[index],
        FieldData<double> doubles => doubles.Data[index],
        FieldData<bool> bools => bools.Data[index],
        _ => null
    };

static ReadOnlyMemory<float> DenseVector(long seed)
{
    var values = new float[DIMENSION];
    for (int i = 0; i < DIMENSION; i++)
    {
        values[i] = ((seed * 7 + i) % 10) / 10f;
    }

    return values;
}

static SparseVector<float> SparseVector(long seed)
{
    int index = (int)(seed % 10);
    return new SparseVector<float>(
        new ReadOnlyMemory<int>(new[] { index, index + 10 }),
        new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f }));
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
    return $"CSHARP_V2_DQL_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Environment.ProcessId}";
}
