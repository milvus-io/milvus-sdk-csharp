# AGENTS.md

Repository guidance for AI agents and contributors working in `milvus-sdk-csharp`.

## Repository layout

Two SDKs live side by side in this repo:

| Project | Assembly | Status | Location |
|---|---|---|---|
| Milvus.Client (V1) | `Milvus.Client.dll` | Legacy, maintained | `Milvus.Client/` |
| Milvus.Client.V2 (V2) | `Milvus.Client.V2.dll` | Current development | `Milvus.Client.V2/` |

- `Milvus.Client.V2` follows the request/response DTO pattern of the Java/C++/Rust V2 SDKs: each operation is a
  `*Req` request and a `*Resp` response, with `ToGrpc*` / `FromGrpc` conversions in
  `Request/` and `Response/`, and shared types in `Types/` and `Utils/`.
- The `Protos/` git submodule holds the shared `milvus.proto` used at build time by both assemblies (neither
  published package references the protos at runtime).

## Docs layout

`docs/` is split by SDK generation:

- `docs/v1/` — legacy V1 docs (api reference, user guide, notebooks, `restful-api.html`, `readme.md`).
- `docs/design/` — design documents. `v2_design.md` is the V2 SDK design.
- `docs/v2/` — V2 API reference and docs; new V2 reference docs go here.

## Building

```bash
dotnet build Milvus.Client.sln
```

- `TreatWarningsAsErrors=true` is on via `Directory.Build.props` — fix warnings, do not suppress them casually.
- `Directory.Packages.props` centralizes package versions.

## Testing

Two test projects, one per SDK. Both use xunit v3.

### Milvus.Client.Tests (V1)

- Runs against a real Milvus via Testcontainers. Select the image with the `MILVUS_IMAGE` env var (default
  `milvusdb/milvus:v2.6.4`).

### Milvus.Client.V2.Tests (V2)

Three layers, tagged with xunit `Category` traits:

| Layer | Tag | Depends on | Examples |
|---|---|---|---|
| Unit | `Category=Unit` | none | request/response DTO conversions, types, enums, utils |
| Integration | `Category=Integration` | in-process gRPC mock (`MockMilvusServer`) | facade request forwarding, error mapping, retry, ts/schema caches |
| System | `Category=System` | real Milvus container (`milvus_container.py`) | end-to-end |

Run a layer with:

```bash
dotnet test Milvus.Client.V2.Tests --filter "Category=Unit"        # or Integration / System
dotnet test Milvus.Client.V2.Tests                                  # all three layers
```

- Integration tests share process-wide singletons (`SchemaCache`, `CollectionTsCache`), so the integration
  assembly disables parallelization (`AssemblyInfo.cs`) and cache-touching tests call `*.Instance.Clear()`
  themselves.
- The `Grpc.*` types in tests resolve via the `Grpc` alias to `Milvus.Client.Grpc` (proto-generated), which is
  declared in `Milvus.Client.V2.csproj` and flows to the test project through the project reference.
- Integration tests cover every public `MilvusClientV2` method (`MockMilvusServer` records each received RPC
  request in its `Requests` dictionary keyed by RPC name).
- System tests start a real Milvus container via `milvus_container.py` (also used as the CI System step).

## Examples

`examples/` is split by SDK generation:

- `examples/v2/` — a single console project (`Milvus.Examples.csproj`, `OutputType=Exe`) referencing
  `Milvus.Client.V2` via `ProjectReference` (no packaging step). It doubles as runnable documentation for the
  V2 API.
- `examples/v1/` — reserved for legacy V1 (`Milvus.Client`) examples.

- One `*Example.cs` class per feature (e.g. `SimpleExample`, `AddFieldExample`, `RBACExample`), each with a
  static `Run(string uri)` entry point; `Program.cs` dispatches by the example name argument.
- `ExampleHelpers.cs` provides `CreateClient` (reads `MILVUS_URI` / `MILVUS_TOKEN`, default `localhost:19530`)
  and `ResetCollectionAsync`.
- Run one with:
  ```bash
  dotnet run --project examples/v2 -- SimpleExample        # or any other example name
  dotnet run --project examples/v2 -- RunAnalyzerExample   # full list in Program.cs
  ```
- `Milvus.Examples.csproj` (under `examples/v2/`) is part of `Milvus.Client.sln` and is compiled by the CI
  `build-v2` job, so example code cannot silently break.

## CI and coverage

- `.github/workflows/Build.yml` splits V1 and V2 into separate jobs:
  - `build-v1`: V1 tests against a matrix of Milvus images (`MILVUS_IMAGE`).
  - `build-v2`: runs Unit, then Integration, then System as three steps.
- Coverage is collected with coverlet (`--collect "XPlat Code Coverage"`) using `coverage.runsettings`, which
  restricts measurement to `Milvus.Client.dll` / `Milvus.Client.V2.dll` and excludes generated code.
  Results are uploaded with `codecov/codecov-action@v5` (anonymous, public repo). `.codecov.yml` ignores
  `obj/`/`bin/`/generated files and enforces the coverage targets.
- Local coverage run:
  ```bash
  dotnet test Milvus.Client.V2.Tests --collect "XPlat Code Coverage" --settings coverage.runsettings
  ```

## Git / PR conventions

- The PR branch must contain exactly one commit; the commit message must carry a `Signed-off-by` trailer
  (DCO check). Squash new work into the single PR commit and force-push with
  `git commit --amend -s --no-edit` / `git push --force-with-lease`.
- Primary remote for upstream is `source` (`milvus-io/milvus-sdk-csharp`); `origin` is the contributor fork.
