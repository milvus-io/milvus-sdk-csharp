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
string collection = $"CSHARP_V2_QUICKSTART_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Environment.ProcessId}";

// MilvusClientV2 connects to the endpoint on first use. `Uri` selects the Milvus endpoint and the
// username/password (or API key) supplies the credential.
Console.WriteLine($"Calling MilvusClientV2: connect to {uri}");
using var client = CreateClient(uri, token);
await client.ConnectAsync();
Console.WriteLine("MilvusClientV2 connected");

try
{
    // CreateCollectionAsync defines the collection schema. The collection name identifies the
    // resource, while the schema describes field types and dimensions.
    Console.WriteLine($"Calling CreateCollectionAsync: create {collection} with schema");
    await client.CreateCollectionAsync(new CreateCollectionReq
    {
        CollectionName = collection,
        Schema = new CollectionSchema
        {
            Fields =
            {
                new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                FieldSchema.CreateVarchar("text", maxLength: 256),
                FieldSchema.CreateFloatVector("embedding", dimension: DIMENSION)
            }
        }
    });
    Console.WriteLine("CreateCollectionAsync completed");

    // CreateIndexAsync builds the vector index that makes the embedding field searchable.
    Console.WriteLine($"Calling CreateIndexAsync: index {collection} on the embedding field");
    await client.CreateIndexAsync(new CreateIndexReq
    {
        CollectionName = collection,
        Indexes =
        [
            new IndexParam("embedding", indexType: IndexType.AutoIndex, metricType: MetricType.Cosine)
        ],
        Sync = true,
        TimeoutMs = 60_000
    });
    Console.WriteLine("CreateIndexAsync completed");

    // InsertAsync writes rows into the selected collection. Each row must contain values compatible
    // with the fields declared above.
    Console.WriteLine($"Calling InsertAsync: write two rows to {collection}");
    await client.InsertAsync(new InsertReq
    {
        CollectionName = collection,
        RowsData =
        [
            new Dictionary<string, object?> { ["id"] = 1L, ["text"] = "hello", ["embedding"] = new[] { 0.1f, 0.2f, 0.3f, 0.4f } },
            new Dictionary<string, object?> { ["id"] = 2L, ["text"] = "milvus", ["embedding"] = new[] { 0.2f, 0.3f, 0.4f, 0.5f } }
        ]
    });
    Console.WriteLine("InsertAsync completed");

    // LoadCollectionAsync prepares the collection for serving queries; Sync waits for readiness.
    Console.WriteLine($"Calling LoadCollectionAsync: load {collection} synchronously");
    await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collection, Sync = true, TimeoutMs = 60_000 });
    Console.WriteLine("LoadCollectionAsync completed");

    // SearchAsync finds nearest vectors. VectorFieldName selects the indexed field, Vectors is the
    // query vector, and Limit caps matches.
    Console.WriteLine("Calling SearchAsync: find the two nearest embedding matches");
    SearchResp response = await client.SearchAsync(new SearchReq
    {
        CollectionName = collection,
        VectorFieldName = "embedding",
        Vectors = [new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f })],
        MetricType = MetricType.Cosine,
        Limit = 2,
        Parameters = new SearchParameters { OutputFields = { "id", "text" } }
    });
    Console.WriteLine("SearchAsync completed");
    foreach (SingleResult single in response.SingleResults ?? [])
    {
        for (int i = 0; i < single.Scores.Count; i++)
        {
            IReadOnlyDictionary<string, object?> row = single.GetRow(i);
            Console.WriteLine($"  id={row["id"]}, score={single.Scores[i]:F4}, text={row["text"]}");
        }
    }
}
finally
{
    // DropCollectionAsync removes the tutorial collection, its data, and its indexes.
    Console.WriteLine($"Calling DropCollectionAsync: remove {collection}");
    await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collection });
    Console.WriteLine("DropCollectionAsync completed");
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
