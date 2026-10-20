# Tutorial 2: Manage the collection lifecycle

This tutorial is a standalone console project that downloads `Milvus.Client.V2` version `2.6.1` from
nuget.org. It demonstrates the collection lifecycle:

1. Create a collection with a description, a vector index, and Bounded consistency.
2. Check existence with `HasCollectionAsync`.
3. Describe the collection to read back its schema and metadata.
4. Add and remove a collection property (TTL).
5. Load and release the collection.
6. Inspect load state and row-count statistics.
7. Rename the collection.
8. Truncate the collection (keep the schema, drop the data).
9. List all collections and clean up.

## Prerequisites

- .NET SDK 8.0.x is installed.
- Milvus is running and accessible.
- `Milvus.Client.V2` version `2.6.1` has been published to nuget.org.

The tutorial uses these environment variables:

| Variable | Default |
|---|---|
| `MILVUS_URI` | `localhost:19530` |
| `MILVUS_TOKEN` | `root:Milvus` |

## Run

From the SDK repository root:

```bash
dotnet run --project tutorial/2_collection
```

Or from this directory:

```bash
dotnet run
```

The program creates uniquely named collections and drops them before exiting normally.
