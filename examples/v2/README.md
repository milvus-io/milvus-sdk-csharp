# Milvus C# SDK (V2) Examples

Runnable, per-feature sample programs that show the Milvus.Client.V2 DTO API in isolation. Each example
is a small console program that connects to a Milvus server, exercises a specific feature area, prints
its progress to the console, and cleans up after itself (collections it creates are dropped on exit, so
re-runs are idempotent).

## Prerequisites

- **.NET SDK 8.0.x** (`dotnet --version`). The repo's `global.json` pins the SDK to `8.0.415` with
  `rollForward: latestFeature`, which only rolls forward within the 8.0.x feature band — a machine with
  only .NET 9/10 SDKs installed cannot build the examples.
- A running **Milvus server**. The examples connect to `localhost:19530` by default. You can start a
  local instance, for example with Docker:

  ```bash
  docker run -d --name milvus \
    -p 19530:19530 -p 9091:9091 \
    -e ETCD_USE_EMBED=true \
    -e COMMON_STORAGETYPE=local \
    milvusdb/milvus:latest
  ```

- A **token** when authentication is enabled (Milvus Standalone with `root:Milvus`). See "Connection"
  below.

## Build and run

The examples live in a single console project that references the **source** `Milvus.Client.V2`
project (`ProjectReference`), so they build and run directly from the repository without any packaging
step.

```bash
cd milvus-sdk-csharp

# Run the default example (SimpleExample):
dotnet run --project examples/v2 -c Debug

# Run a specific example by name:
dotnet run --project examples/v2 -c Debug -- GeneralExample
dotnet run --project examples/v2 -c Debug -- JsonFieldExample

# List all available examples (pass an unknown name):
dotnet run --project examples/v2 -c Debug -- xyz
```

### Connection

The examples read two environment variables. Both are optional in general, except `RBACExample`, which
requires an auth-enabled server: it needs `MILVUS_TOKEN` set (and Milvus started with
`MILVUS_AUTHORIZATION_ENABLED=true`), otherwise it prints a message and does nothing.

| Variable       | Default           | Meaning                                                              |
|----------------|-------------------|----------------------------------------------------------------------|
| `MILVUS_URI`   | `localhost:19530` | The Milvus `host:port` to connect to.                                |
| `MILVUS_TOKEN` | *(none)*          | `username:password` (e.g. `root:Milvus`) or a raw API key when auth is on. |

```bash
MILVUS_URI=192.168.1.10:19530 MILVUS_TOKEN=root:Milvus \
  dotnet run --project examples/v2 -c Debug -- SparseVectorExample
```

Each example prints what it is doing, so the console output doubles as the expected behavior of the
underlying feature.

## Example index

| Example                 | Demonstrates                                                                 |
|-------------------------|------------------------------------------------------------------------------|
| `SimpleExample`         | End-to-end quickstart: connect, create a collection, create an index, insert, load, search, drop. |
| `GeneralExample`        | A broader tour: schema with several field types, insert, query, search, upsert, delete. |
| `AddFieldExample`       | Adding a new field to a collection's schema after creation, then inserting rows that include it. |
| `ArrayFieldExample`     | Array fields: schema with an int-array field, insert array rows, query them back. |
| `JsonFieldExample`      | JSON fields: schema with a JSON field, insert JSON rows, filter on a JSON key. |
| `SparseVectorExample`   | Sparse vectors: insert `MilvusSparseVector` rows and search over them. |
| `Float16VectorExample`  | Float16 vectors: insert half-precision rows (via `Float16Utils`) and search with a float16 query. |
| `Int8VectorExample`     | Int8 vectors: insert `sbyte` rows and query them back. |
| `BinaryVectorExample`   | Binary vectors: insert bit-packed rows and query them back. |
| `UpsertExample`         | Upsert (insert-or-update): update an existing row and add a new one in one call. |
| `GroupByExample`        | Group-by search: return one hit per distinct field value. |
| `PartitionKeyExample`   | Partition-key fields: schema with a partition-key field and a search filtered by partition key. |
| `DynamicFieldExample`   | Dynamic fields: insert rows with fields that are not in the schema (`EnableDynamicFields`). |
| `NullableFieldExample`  | Nullable fields with default values.                                        |
| `ConsistencyLevelExample` | Consistency levels, including Session (read-your-writes via the ts cache) and Strong. |
| `RunAnalyzerExample`    | The text analyzer (`RunAnalyzerAsync`) and analyzed tokens.                 |
| `AliasExample`          | Collection aliases: create, list, drop.                                    |
| `RBACExample`           | Users, roles and privileges (create, grant, list, revoke, drop).            |
| `FullTextSearchExample` | BM25 full-text search: a BM25 function over a VARCHAR field, searching with plain text and a `TEXT_MATCH` keyword filter. |
| `HybridSearchExample`   | Hybrid search: dense + sparse ANN sub-requests fused with a weighted reranker. |
| `RankerExample`         | Function-score rerankers (boost/decay) applied to search results.          |
| `IteratorExample`       | Server-side query/search iterators: page over large result sets with `await foreach`. |
| `DefaultValueExample`   | Field default values: rows that omit a field get its default filled in.    |
| `FilterTemplateExample` | Filter templates: `{alias}` placeholders in expressions, values supplied separately. |
| `DBExample`             | Databases: create/list/describe/drop, `UseDatabaseAsync`, request-level `DatabaseName`. |
| `NullableVectorExample` | Nullable vector fields: insert null vectors and add nullable vector fields to existing collections. |
| `MultiAnalyzerExample`  | Multi-analyzers on one VARCHAR field (per-language tokenizers), BM25 search. |
| `TimestampExample`      | Timestamptz fields: insert ISO-8601 timestamps and query them in a timezone. |
| `GeometryExample`       | Geometry fields: insert WKT geometries and filter with spatial predicates (ST_WITHIN, ...). |
| `StructExample`         | Array-of-Struct fields: insert struct rows and search a nested vector sub-field via `EmbeddingList`. |
| `OptimizeExample`       | Collection optimization (`OptimizeAsync`): major compaction to a target segment size. |
| `CDCExample`            | Cross-cluster replication (CDC): configure replication topology between two clusters. |
| `HighlighterExample`    | Lexical highlighting on BM25 full-text search results (pre/post tags, fragment size). |
| `TLSExample`            | TLS connections: one-way via `https://` URI, mutual TLS via `ChannelOptions`. |

## Code organization

- `Program.cs` — entry point; dispatches to an example by name (first CLI argument, default
  `SimpleExample`).
- `ExampleHelpers.cs` — shared helpers: builds a `MilvusClientV2` from `MILVUS_URI`/`MILVUS_TOKEN`, and
  resets a collection so examples are idempotent.
- `*Example.cs` — one file per feature; each exposes a `static Task Run(string uri)` and mirrors the
  corresponding example in the C++ (`examples/src/v2/`) and Java (`io.milvus.v2`) SDKs.
