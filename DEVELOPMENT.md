# Developing the Milvus C# SDK

New SDK functionality belongs in the V2 API (`Milvus.Client.V2`). The original V1 API
(`Milvus.Client`) is maintained for compatibility and should receive only compatibility,
correctness, or security fixes.

## Prerequisites

- .NET SDK `8.0.415` (see [global.json](global.json), which pins the SDK with
  `rollForward: latestFeature`)
- Git submodules initialized
- Linux server-backed tests: Python 3 and access to a running Docker daemon

The protobuf compiler is provided through the `Grpc.Tools` NuGet package, so a system-installed
`protoc` is not required.

`global.json` pins the SDK to `8.0.415` with `rollForward: latestFeature`, which only rolls forward
within the 8.0.x feature band — a machine with only .NET 9/10 SDKs installed cannot build this
repository. Changes that require a newer SDK must update `global.json`, user documentation, and the
CI check together.

Initialize the repository submodules:

```bash
git submodule update --init --recursive
```

## Build

Build the solution (both SDK assemblies, the examples, and both test projects):

```bash
dotnet build Milvus.Client.sln
```

`TreatWarningsAsErrors=true` is enabled via `Directory.Build.props` — fix warnings, do not suppress
them casually. `Directory.Packages.props` centralizes package versions.

Generated protobuf bindings are written to each project's `obj/` folder by `Grpc.Tools`. Do not edit
generated files under `obj/` or `bin/`.

## Tests without Milvus

The V2 test project has three layers, tagged with xunit `Category` traits. The Unit and Integration
layers do not require Milvus (Integration uses an in-process gRPC mock, `MockMilvusServer`):

```bash
dotnet test Milvus.Client.V2.Tests --filter "Category=Unit"
dotnet test Milvus.Client.V2.Tests --filter "Category=Integration"
```

Run the repository's complete non-server validation path with:

```bash
./scripts/run_tests.sh --ut --it
```

This compiles the solution, deletes `bin`/`obj` so a stale `genruntimeconfig.cache` cannot leave the
test assembly without its runtimeconfig.json, and drives the xunit v3 in-process runner directly
(`dotnet exec`), which is reliable on hosts where the VSTest adapter fails.

## Server-backed tests

The V1 test project and the V2 System layer require a real Milvus. On Linux, the recommended command
manages a standalone Milvus container automatically:

```bash
./scripts/run_tests.sh --all        # Unit + Integration + System (default)
./scripts/run_tests.sh --st         # System layer only
```

The V1 tests select their Milvus image with the `MILVUS_IMAGE` env var (default
`milvusdb/milvus:v2.6.4`); the V2 System layer starts the repository-managed container through
`Milvus.Client.V2.Tests/milvus_container.py`. CI uses `dotnet test` (required for coverage
collection); prefer `scripts/run_tests.sh` for local runs.

If direct server-backed tests report gRPC timeouts, verify that Milvus is healthy and has enough CPU
and memory, then retry.

## Tutorials and examples

Examples under `examples/v2` and tutorials under `tutorial/` may create, modify, and delete Milvus
resources. Use a disposable or explicitly approved server.

Run one example or tutorial against a live server:

```bash
dotnet run --project examples/v2 -- SimpleExample
dotnet run --project tutorial/1_quickstart
```

To run every example against a live server, use `scripts/run_examples.sh`. To start the
repository-managed standalone server and run every tutorial on Linux, use:

```bash
./scripts/run_tutorials.sh
```

The tutorials are standalone console projects that consume the **published** `Milvus.Client.V2`
package from nuget.org rather than the SDK source tree, mirroring the experience of an application
developer. Their `tutorial/Directory.Build.props` isolates them from the repository's central
package management and warning-as-errors analyzers.

The RBAC tutorial can exercise management calls on the default authorization-disabled server, but
permission enforcement requires a separately configured authorization-enabled Milvus instance.

## Optional diagnostics

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

## Formatting and code quality

`TreatWarningsAsErrors=true`, `Nullable`, `ImplicitUsings`, and `AnalysisLevel=latest` are enabled
via `Directory.Build.props` for the SDK and test projects. A clean `dotnet build Milvus.Client.sln`
is the primary gate — fix warnings rather than suppressing them.

Tutorials are standalone projects and are intentionally excluded from those analyzers
(`tutorial/Directory.Build.props` sets `AnalysisLevel=none` and `TreatWarningsAsErrors=false`).

## Coverage

Coverage is collected with coverlet via `coverage.runsettings`, which restricts measurement to
`Milvus.Client.dll` and `Milvus.Client.V2.dll` and excludes generated protobuf/gRPC code:

```bash
dotnet test Milvus.Client.V2.Tests --collect "XPlat Code Coverage" --settings coverage.runsettings
```

Results are uploaded with `codecov/codecov-action@v5`; `.codecov.yml` ignores `obj/`/`bin/`/generated
files and enforces the coverage targets.

## Protobuf regeneration and clean builds

The SDK compiles protobuf sources from the `Protos` submodule through `Grpc.Tools` into each
project's build output. To force regeneration after changing protobuf or build inputs:

```bash
rm -rf Milvus.Client/bin Milvus.Client/obj Milvus.Client.V2/bin Milvus.Client.V2/obj
dotnet build Milvus.Client.sln
```

Do not commit files generated under `bin/` or `obj/`.

## Debugging

For a failing managed server run, `scripts/run_tests.sh` and `scripts/run_tutorials.sh` print recent
Milvus container logs before cleanup. Set `MILVUS_URI`/`MILVUS_TOKEN` to point at a specific server,
or attach `LoggerFactory` (see [Optional diagnostics](#optional-diagnostics)) to trace individual
RPCs.

## Releasing

See the `NugetPublish.yml` workflow for the automated publish path: a `v*.*.*` tag push triggers
build, pack and push of `Milvus.Client` / `Milvus.Client.V2` (plus `.snupkg`) to nuget.org via OIDC
(Trusted Publishing), then creates a GitHub release. The `validate` job requires the tagged commit to
be on a remote branch with green CI checks, and the tagged version must have a `CHANGELOG.md` entry.
Do not modify `NugetPublish.yml` casually — it is the only automated publish path.
