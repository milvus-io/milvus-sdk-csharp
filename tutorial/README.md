# Milvus C# SDK tutorials

This directory contains beginner-oriented tutorials for the Milvus C# SDK (V2).

Each tutorial is an independent .NET console project that depends on the published
`Milvus.Client.V2` NuGet package rather than the SDK source tree. This keeps the tutorials close to the
experience of an application developer installing the SDK from nuget.org.

Run a tutorial from the repository root with:

```bash
dotnet run --project tutorial/<tutorial-name>
```

## Prerequisites

- .NET SDK 8.0.x (`dotnet --version`).
- A running Milvus server. Tutorials expect Milvus at `localhost:19530` by default. For a different
  server, set `MILVUS_URI` and `MILVUS_TOKEN`:

  ```bash
  MILVUS_URI=192.168.1.10:19530 MILVUS_TOKEN=root:Milvus \
    dotnet run --project tutorial/1_quickstart
  ```

  To start a disposable standalone Milvus container and run every tutorial, use
  `scripts/run_tutorials.sh`. It is intended for Linux and requires Docker.

## Start here

If this is your first Milvus C# SDK program, run the quick start first:

```bash
dotnet run --project tutorial/1_quickstart
```

## Beginner tutorials

- [`1_quickstart`](1_quickstart/): connect, create a collection, insert data, search, and clean up.
- [`2_collection`](2_collection/): manage the collection lifecycle.
- [`3_schema`](3_schema/): define collection fields for the supported V2 data types.
- [`4_index`](4_index/): create and inspect vector and scalar indexes.
- [`5_dml`](5_dml/): insert, upsert, and delete collection data.
- [`6_dql`](6_dql/): query, search, hybrid search, and iterate through results.

## Advanced tutorials

- [`7_database`](7_database/): create, configure, select, and remove databases.
- [`8_rbac`](8_rbac/): manage users, roles, privilege groups, and grants.

The advanced tutorials change server-level resources. Run them only with an administrative credential
and on a disposable or explicitly approved Milvus instance.

## Common troubleshooting

- `connection refused`: Milvus is not running at `MILVUS_URI`, or the endpoint is not reachable.
- `unauthenticated` or `permission denied`: `MILVUS_TOKEN` is missing, invalid, or lacks permission for
  the requested resource.
- load/index timeout: verify that Milvus is healthy and has enough CPU and memory, then retry.
- `port is already allocated` from `scripts/run_tutorials.sh`: another container or tutorial run is
  using ports `29530` or `29091`; stop it or override `MILVUS_GRPC_PORT` and `MILVUS_HEALTH_PORT`.
- Docker daemon errors: start Docker and ensure the current user can run `docker info`.
