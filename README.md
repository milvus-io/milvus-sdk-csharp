# Milvus C# SDK

The official C# SDK for [Milvus](https://milvus.io/).

<div class="column" align="middle">
  <a href="https://milvusio.slack.com/archives/C053HTUQGUC"><img src="https://img.shields.io/badge/Join-Slack-orange?logo=slack&amp;logoColor=white&style=flat-square"></a>
  <img src="https://img.shields.io/nuget/v/milvus.client.v2"/>
  <img src="https://img.shields.io/nuget/dt/milvus.client.v2"/>
  <img src="https://img.shields.io/nuget/v/milvus.client"/>
  <img src="https://img.shields.io/nuget/dt/milvus.client"/>
</div>

<div align="middle">
    <img src="milvussharp.png"/>
</div>

## Compatibility

The following table lists the recommended Milvus C# SDK version for each Milvus release line:

| Milvus version | Recommended SDK version |
|:---:|:---:|
| 2.2.x | `v2.2.2` (V1) |
| 2.3.x | `release/2.3` branch (V1) |
| 2.6.x | `v2.6.0` (V2) |

Two generations of the SDK live in this repository:

| SDK | Assembly | Status |
| --- | --- | --- |
| **V2** — `Milvus.Client.V2` | `Milvus.Client.V2.dll` | Current; request/response DTO API aligned with the Java/C++/Rust V2 SDKs |
| **V1** — `Milvus.Client` | `Milvus.Client.dll` | Legacy, maintained for compatibility |

See [CHANGELOG.md](CHANGELOG.md) for release-specific features, fixes, and compatibility notes.

Supported .NET versions:

- .NET 8 (V2)
- .NET Core 2.1+ (V1)
- .NET Framework 4.6.1+ (V1)

## Use the SDK in your project

### Choose the client API

New applications should use `MilvusClientV2`, which provides the current request/response-style API.
The legacy `MilvusClient` API is retained for compatibility with existing applications and is in
maintenance mode.

### Add the NuGet package

```bash
dotnet add package Milvus.Client.V2
```

For the legacy V1 SDK:

```bash
dotnet add package Milvus.Client
```

### Minimal MilvusClientV2 program

The following program connects to Milvus and then closes the connection:

```csharp
using Milvus.Client.V2;
using Milvus.Client.V2.Types;

using var client = new MilvusClientV2(new ConnectConfig { Uri = "localhost:19530" });
await client.ConnectAsync();
```

This program expects Milvus at `localhost:19530`. For an independently buildable project that covers
collection creation, insertion, loading, search, and cleanup, follow
[Tutorial 1: Quickstart](tutorial/1_quickstart/).

## Tutorials

Standalone console projects are available under [`tutorial`](tutorial/README.md). They consume the
published `Milvus.Client.V2` NuGet package as an application dependency (like an app that installs the
SDK from nuget.org) and provide a beginner-to-advanced sequence:

1. [`Quick start`](tutorial/1_quickstart/)
2. [`Collection`](tutorial/2_collection/)
3. [`Schema`](tutorial/3_schema/)
4. [`Index`](tutorial/4_index/)
5. [`DML`](tutorial/5_dml/)
6. [`DQL`](tutorial/6_dql/)
7. [`Database (advanced)`](tutorial/7_database/)
8. [`RBAC (advanced)`](tutorial/8_rbac/)

Each tutorial has its own prerequisites, configuration, run command, expected output, and
troubleshooting guidance. Start with the [`tutorial index`](tutorial/README.md) for shared
connection settings and the recommended learning path.

Run one tutorial from the repository root with:

```bash
dotnet run --project tutorial/1_quickstart
```

On Linux, repository maintainers can start a disposable standalone Milvus server and run every
tutorial with:

```bash
./scripts/run_tutorials.sh
```

## More examples

Additional V1 and V2 examples are available under [`examples`](examples). New applications should
start with the V2 examples in [`examples/v2`](examples/v2). Each example is a small console program
that connects to a server, exercises one feature area, and cleans up after itself.

Run an example by its name:

```bash
dotnet run --project examples/v2 -- SimpleExample
```

Examples connect to Milvus and may create, modify, or delete resources. Review the selected example
and its connection settings before running it.

## NuGet

`Milvus.Client` (V1) and `Milvus.Client.V2` (V2) are delivered via the NuGet package manager.

| NuGet version | Branch | Description | Milvus version |
| --- | --- | --- | --- |
| v2.6.0 | 2.6 | V2 `Milvus.Client.V2` first stable release | 2.6.x |
| v2.2.2 | main | Support grpc only | 2.2.x |
| v2.2.1 | main | Support restfulapi and grpc[**Obsolete**] | 2.2.x |
| v2.2.0 | 2.2 | Support grpc only[**Obsolete**] | 2.2.x |

## Troubleshooting

- `connection refused`: start Milvus or set `MILVUS_URI` to a reachable endpoint.
- `unauthenticated` or `permission denied`: set `MILVUS_TOKEN` to a valid API key or
  `username:password` credential with permission for the requested operation.
- load or index timeout: verify that Milvus is healthy and has enough CPU and memory, then retry.
- Docker or port errors from repository scripts: ensure `docker info` succeeds and the required
  ports are available.

## Optional SDK diagnostics

The V2 client logs through `Microsoft.Extensions.Logging`. Provide a `LoggerFactory` on the
`ConnectConfig` to observe retry attempts and RPC failures:

```csharp
using Microsoft.Extensions.Logging;
using Milvus.Client.V2;
using Milvus.Client.V2.Types;

var loggerFactory = LoggerFactory.Create(builder => builder
    .AddSimpleConsole()
    .SetMinimumLevel(LogLevel.Debug));

var config = new ConnectConfig
{
    Uri = "localhost:19530",
    LoggerFactory = loggerFactory
};
```

(`AddSimpleConsole` requires the `Microsoft.Extensions.Logging.Console` package.)

The client logs health-check failures (Warning), the invocation and argument of each operation
(Debug), and operation failures with their error code (Error). It never logs credentials, request
payloads, filters, or vector data.

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for development setup, builds, mock and server-backed tests,
Docker automation, coverage, and protobuf regeneration.

## Docs

* [V1 Quick Start](./docs/v1/readme.md)
* [V2](./docs/v2/README.md)
* [V2 design](./docs/design/v2_design.md)
* [V2 examples](./examples/v2/README.md)
* [Milvus docs](https://milvus.io/docs)

### Jupyter Notebooks (V1)

You can find legacy V1 (`Milvus.Client`) Jupyter notebooks in the
[docs/v1/notebooks](./docs/v1/notebooks) folder.

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/milvus-io/milvus-sdk-csharp)

* [00.Settings.ipynb](./docs/v1/notebooks/00.Settings.ipynb)
* [01.Connect to milvus.ipynb](./docs/v1/notebooks/01.Connect%20to%20milvus.ipynb)
* [02.Create a Collection.ipynb](./docs/v1/notebooks/02.Create%20a%20Collection.ipynb)
* [03.Create a Partition.ipynb](./docs/v1/notebooks/03.Create%20a%20Partition.ipynb)
* [04.Insert Vectors.ipynb](./docs/v1/notebooks/04.Insert%20Vectors.ipynb)
* [05.Build an Index on Vectors.ipynb](./docs/v1/notebooks/05.Build%20an%20Index%20on%20Vectors.ipynb)
* [06.Search.ipynb](./docs/v1/notebooks/06.Search.ipynb)
* [07.Query.ipynb](./docs/v1/notebooks/07.Query.ipynb)

> Requirements: C# notebooks require .NET 7 and the VS Code Polyglot extension.

## License

[Apache License 2.0](LICENSE)
