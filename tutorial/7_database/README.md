# Tutorial 7: Manage databases (advanced)

This tutorial is a standalone console project that downloads `Milvus.Client.V2` version `2.6.1`
from nuget.org. It demonstrates how to:

1. Connect with `MilvusClientV2`.
2. List databases.
3. Create and describe a database.
4. Add and remove a database property.
5. Select a database with `UseDatabaseAsync`.
6. Return to the default database and clean up.

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
dotnet run --project tutorial/7_database
```

Or from this directory:

```bash
dotnet run
```

The program creates a uniquely named database and drops it before exiting normally.
