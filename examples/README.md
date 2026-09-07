# Milvus C# SDK (V2) Examples

Runnable, per-feature sample programs that show the Milvus.Client.V2 DTO API in isolation. Each example
is a small console program that connects to a Milvus server, exercises a specific feature area, prints
its progress to the console, and cleans up after itself (collections it creates are dropped on exit, so
re-runs are idempotent).

## Prerequisites

- **.NET SDK 8.0** or later (`dotnet --version`).
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
dotnet run --project examples -c Debug

# Run a specific example by name:
dotnet run --project examples -c Debug -- GeneralExample
dotnet run --project examples -c Debug -- JsonFieldExample

# List all available examples (pass an unknown name):
dotnet run --project examples -c Debug -- xyz
```

### Connection

The examples read two environment variables; both are optional.

| Variable       | Default           | Meaning                                                              |
|----------------|-------------------|----------------------------------------------------------------------|
| `MILVUS_URI`   | `localhost:19530` | The Milvus `host:port` to connect to.                                |
| `MILVUS_TOKEN` | *(none)*          | `username:password` (e.g. `root:Milvus`) or a raw API key when auth is on. |

```bash
MILVUS_URI=192.168.1.10:19530 MILVUS_TOKEN=root:Milvus \
  dotnet run --project examples -c Debug -- SparseVectorExample
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
| `BinaryVectorExample`   | Binary vectors: insert bit-packed rows and search. |
| `UpsertExample`         | Upsert (insert-or-update): update an existing row and add a new one in one call. |
| `GroupByExample`        | Group-by search: return one hit per distinct field value. |
| `PartitionKeyExample`   | Partition-key fields: schema with a partition-key field and a search filtered by partition key. |
| `DynamicFieldExample`   | Dynamic fields: insert rows with fields that are not in the schema (`EnableDynamicFields`). |
| `NullableFieldExample`  | Nullable fields with default values.                                        |
| `ConsistencyLevelExample` | Consistency levels, including Session (read-your-writes via the ts cache) and Strong. |
| `RunAnalyzerExample`    | The text analyzer (`RunAnalyzerAsync`) and analyzed tokens.                 |
| `AliasExample`          | Collection aliases: create, list, drop.                                    |
| `RBACExample`           | Users, roles and privileges (create, grant, list, revoke, drop).            |

## Code organization

- `Program.cs` — entry point; dispatches to an example by name (first CLI argument, default
  `SimpleExample`).
- `ExampleHelpers.cs` — shared helpers: builds a `MilvusClientV2` from `MILVUS_URI`/`MILVUS_TOKEN`, and
  resets a collection so examples are idempotent.
- `*Example.cs` — one file per feature; each exposes a `static Task Run(string uri)` and mirrors the
  corresponding example in the C++ (`examples/src/v2/`) and Java (`io.milvus.v2`) SDKs.
