# Tutorial 1: Quick start

This is the smallest complete Milvus application in the C# SDK. It connects to Milvus, creates a
collection, inserts a few rows, searches a vector, prints the results, and removes the collection
before exiting.

## Prerequisites

- .NET SDK 8.0.x is installed.
- Milvus 2.6 or later is running and accessible.
- `Milvus.Client.V2` version `2.6.1` has been published to nuget.org.

Connection settings use `MILVUS_URI` and `MILVUS_TOKEN`, defaulting to `localhost:19530` and
`root:Milvus`.

## Run

```bash
dotnet run --project tutorial/1_quickstart
```

The collection name is unique for each run. The program cleans it up on normal completion.

## Expected output

The IDs and scores vary, but the run should include messages like:

```text
Calling MilvusClientV2: connect to localhost:19530
MilvusClientV2 connected
Calling CreateCollectionAsync: create CSHARP_V2_QUICKSTART_... with schema
CreateCollectionAsync completed
Calling CreateIndexAsync: index CSHARP_V2_QUICKSTART_... on the embedding field
CreateIndexAsync completed
Calling InsertAsync: write two rows to CSHARP_V2_QUICKSTART_...
InsertAsync completed
Calling LoadCollectionAsync: load CSHARP_V2_QUICKSTART_... synchronously
LoadCollectionAsync completed
Calling SearchAsync: find the two nearest embedding matches
SearchAsync completed
  id=1, score=1.0000, text=hello
  id=2, score=0.9938, text=milvus
Calling DropCollectionAsync: remove CSHARP_V2_QUICKSTART_...
DropCollectionAsync completed
```

## Troubleshooting

- `connection refused`: start Milvus or set `MILVUS_URI` to the reachable endpoint.
- `unauthenticated` or `permission denied`: set a valid `MILVUS_TOKEN`.
- load timeout: check that the Milvus server is healthy and has enough resources.
