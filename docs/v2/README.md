# Milvus C# SDK (V2) — `Milvus.Client.V2`

The second-generation .NET SDK for [Milvus](https://milvus.io), the open-source vector database.
It implements the request/response DTO API (mirroring the Java/C++/Rust V2 SDKs) over gRPC and
targets `net8.0`, `netstandard2.0` and `net462`.

## Install

```bash
dotnet add package Milvus.Client.V2
```

## Quickstart

```csharp
using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

// Connect (see below for a token when authentication is enabled).
using var client = new MilvusClientV2(new ConnectConfig { Uri = "localhost:19530" });

// Build the collection schema with its fields.
var schema = new CollectionSchema { Name = "book" };
schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true, autoId: true));
schema.Fields.Add(FieldSchema.CreateVarchar("title", maxLength: 256));
schema.Fields.Add(FieldSchema.CreateFloatVector("embedding", dimension: 128));

// Create the collection.
await client.CreateCollectionAsync(new CreateCollectionReq
{
    CollectionName = "book",
    Schema = schema
});

// Insert rows (row-based or column-based; here row-based).
await client.InsertAsync(new InsertReq
{
    CollectionName = "book",
    RowsData =
    [
        new Dictionary<string, object?>
        {
            ["title"] = "The Time Machine",
            ["embedding"] = Enumerable.Repeat(0.1f, 128).ToArray()
        }
    ]
});

// Build an index and load before searching.
await client.CreateIndexAsync(new CreateIndexReq
{
    CollectionName = "book",
    Indexes = [new IndexParam("embedding", indexType: IndexType.AutoIndex, metricType: SimilarityMetricType.Cosine)]
});
await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = "book" });

// Search.
SearchResp results = await client.SearchAsync(new SearchReq
{
    CollectionName = "book",
    VectorFieldName = "embedding",
    Vectors = [Enumerable.Repeat(0.1f, 128).ToArray()],
    MetricType = SimilarityMetricType.Cosine,
    Limit = 10
});
```

See `docs/design/v2_design.md` for the full design and the request/response DTO conventions, and the
`examples/v2/` project for runnable, per-feature sample programs (each one is a small console app that
connects to a server, exercises one feature area, and cleans up after itself).

## Connection

```csharp
// Default port 19530, plaintext.
using var client = new MilvusClientV2(new ConnectConfig { Uri = "localhost:19530" });

// With authentication (username/password) and TLS (https:// scheme).
using var client = new MilvusClientV2(new ConnectConfig
{
    Uri = "https://host:19530",
    Username = "root",
    Password = "Milvus"
});

// With an API key instead.
using var client = new MilvusClientV2(new ConnectConfig { Uri = "https://host:19530", ApiKey = "..." });

// Connect to a specific database.
using var client = new MilvusClientV2(new ConnectConfig { Uri = "localhost:19530", Database = "my_db" });
```

The examples honor the `MILVUS_URI` and `MILVUS_TOKEN` environment variables
(default `localhost:19530` / none).

## Documentation layout

| Path | Contents |
|---|---|
| `docs/v2/` | V2 API reference and docs (this readme is packed into the NuGet package). |
| `docs/v1/` | Legacy first-generation SDK (`Milvus.Client`) docs. |
| `docs/design/v2_design.md` | V2 design document. |
| `examples/v2/` | Runnable V2 example programs. |
