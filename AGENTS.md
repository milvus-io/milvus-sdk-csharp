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
- `global.json` pins the SDK to `8.0.415` (`rollForward: latestFeature`), which only rolls forward within the
  8.0.x feature band — a machine with only .NET 9/10 SDKs cannot build the repo.

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
- Prefer `scripts/run_tests.sh` for local runs (`--ut` / `--it` / `--st` / `--all`): it drives the xunit v3
  in-process runner directly (`dotnet exec`), which is reliable on hosts where the `dotnet test` VSTest adapter
  fails with "Test process did not return valid JSON (non-object)". CI uses `dotnet test` because it works there
  and is required for coverage collection. Both scripts delete `bin`/`obj` before building so a stale
  `genruntimeconfig.cache` cannot leave the test assembly without its runtimeconfig.json.

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
- Or drive every example against a live server with `scripts/run_examples.sh` (`--list` to enumerate names, or
  pass one or more example names to run only those; TLSExample/CDCExample are excluded from the default run).
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

## NuGet publishing (Trusted Publishing)

- `.github/workflows/NugetPublish.yml` is the **only** automated publish path: a `v*.*.*` tag push triggers
  build, pack and push of `Milvus.Client` / `Milvus.Client.V2` (plus `.snupkg`) to nuget.org via OIDC
  (Trusted Publishing, `NuGet/login@v1`), then creates a GitHub release. A `validate` job first requires the
  tagged commit to be on `main` with a green CI run, and `Version.props` is overridden from the tag by
  `TagToVersion` (the tag version must match the resulting `Version.props`). The tagged version must also
  have a `CHANGELOG.md` entry (validated by `scripts/extract_changelog.sh`), which is used as the GitHub
  release body. Do **not** modify it casually.
  Both packages are packed explicitly so the examples/test projects are never published.
- The nuget.org Trusted Publishing policy is pinned to `milvus-io/milvus-sdk-csharp` + the `NugetPublish.yml`
  workflow filename, so **only that exact repo + workflow** can obtain the temporary API key. `NUGET_USER` is a
  username, not a credential; the actual gate is GitHub's signed OIDC token. Manual `dotnet nuget push` still
  requires a nuget.org API key and is out of band.
- **Be wary of new or renamed workflows.** Any `.github/workflows/*.yml` that could run in this repo with
  `id-token: write` and push a `.nupkg` — or any rename of `NugetPublish.yml` — is a potential package
  impersonation vector, because the policy matches on the repository and the workflow filename. Review such
  changes as supply-chain risks.
- When adding or editing any workflow, check whether it could (directly or transitively) publish packages,
  expose the `NUGET_USER` secret, or trigger the publish path; do not fold publish logic into unrelated
  workflows.

## Git / PR conventions

- The PR branch must contain exactly one commit; the commit message must carry a `Signed-off-by` trailer
  (DCO check). Squash new work into the single PR commit and force-push with
  `git commit --amend -s --no-edit` / `git push --force-with-lease`.
- Primary remote for upstream is `source` (`milvus-io/milvus-sdk-csharp`); `origin` is the contributor fork.
