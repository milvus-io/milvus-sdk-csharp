# Milvus C# SDK (V2)

> **文档目录约定**
>
> - `docs/v1/` — 第一代 SDK（`Milvus.Client`）的原有文档（api reference / user guide / notebooks 等）。
> - `docs/design/` — 设计文档。本文 `v2_design.md` 是 V2 SDK 的设计说明。
> - `docs/v2/` — 第二代 SDK（`Milvus.Client.V2`）的 API reference 等文档，后续 V2 的 reference 请放到此目录。

## 0. Why V2 — limitations of V1 (`Milvus.Client`)

The first-generation SDK (`Milvus.Client`) works, but its design does not scale cleanly to the full
Milvus API surface and diverges from the other official SDKs. V2 exists to address the following
concrete deficiencies (all observed in the V1 source, branch `main`):

1. **Constructor overload explosion** — `MilvusClient` has **9 public constructors**
   (`MilvusClient.cs:31,56,84,...`) covering host/port, username/password, api-key, URI and
   `GrpcChannel` variants in every combination. Adding a configuration knob means adding more
   overloads.

2. **Long, positional parameter lists instead of request objects** — e.g. `SearchAsync` has **3
   overloads** whose signatures are `(vectorFieldName, vectors, metricType, limit, parameters,
   cancellationToken)` (`MilvusCollection.Entity.cs:182,235,288`); `CreateCollectionAsync` takes
   `(collectionName, schema, consistencyLevel, shardsNum, cancellationToken)`
   (`MilvusClient.Collection.cs:61`). Every optional feature adds a positional parameter, hurting
   readability, extensibility and binary compatibility.

3. **Mutable handle objects instead of a single facade** — operations are scattered over the client
   and mutable stateful handles (`MilvusCollection`, `MilvusPartition`, `MilvusCollection.cs:17
   Name { get; private set; }`). There is no one "facade" a caller learns; state (e.g. which
   collection) is carried implicitly by the handle.

4. **Flat 49-type public namespace** — all `Milvus.Client` public types (client, collections,
   schema, parameters, responses, exceptions) live in a single flat namespace with no sub-namespace
   organization, making the public API hard to navigate.

5. **Ad-hoc Session-consistency bookkeeping** — the last-DML timestamp is tracked by an
   unsynchronized `CollectionLastMutationTimestamps` dictionary field on the client
   (`MilvusClient`, `MilvusCollection.Entity.cs:48,83,158,1144`) rather than a proper cache, and
   there is no schema cache and no retry policy anywhere.

6. **No retry mechanism** — `InvokeAsync` performs a single call and maps `common.Status` to
   `MilvusException`; transient failures (e.g. rate limit) are never retried, unlike Java/C++/PyMilvus.

7. **Thread-unsafe shared mutable state** — the mutation-timestamp dictionary is mutated from any
   DML without synchronization.

8. **Incomplete error surface** — `MilvusErrorCode` has only 7 values and `MilvusException` carries
   no structured retryable-vs-nonretryable information.

9. **Inconsistent response shapes** — many operations return bare `Task` or ad-hoc values rather
   than typed response objects, so callers cannot uniformly consume results.

10. **Poor extensibility of the existing API surface** — adding brand-new methods is easy (the
    client/collection classes are `partial`), but **extending existing APIs is structurally hard**:
    every new feature is bolted on as another positional optional parameter
    (`FieldSchema.CreateVarchar` grew from 6 to 8 parameters for nullable/default support, and more
    for analyzers), which hurts readability and binary compatibility (C# optional parameters are
    bound at the call site); configuration is added via yet another constructor overload (9 today);
    and there are no interception/DI extension points, so cross-cutting concerns (logging, auth,
    retry) are hard-coded and cannot be extended from outside the library.

V2 keeps V1 working and untouched, but introduces the **DTO request/response pattern**, a single
`MilvusClientV2` facade, explicit `ConnectAsync`, proper schema/ts caches and a retry policy — the
same architecture the Java/C++/Rust/PyMilvus SDKs already use. The DTO pattern in particular
directly answers point 10: a new parameter is a new property on a `*Req` class, never a change to
an existing method signature.

## 1. Overview

This document describes the design of the Milvus .NET SDK, specifically the second-generation
(`Milvus.Client.V2`) implementation that coexists with the existing first-generation
(`Milvus.Client`) package.

The V2 implementation follows the same **DTO (Data Transfer Object) pattern** used by the
Java/C++/Rust Milvus SDKs: every operation is expressed as a request object (`*Req`), executed by
a single facade class (`MilvusClientV2`), and returns a typed response object (`*Resp`).

### 1.1 Goals

- **Backward compatibility**: keep `Milvus.Client` (V1) and its tests untouched; publishing V2 must
  not affect existing V1 consumers.
- **Zero coupling**: `Milvus.Client.V2` is a standalone assembly with its own namespace, package,
  and proto code generation. A consumer referencing only `Milvus.Client.V2` never needs V1.
- **Parity with Java/C++/PyMilvus V2 SDKs**: same facade name, same DTO pattern, same feature scope,
  same error/retry/cache semantics. The first-phase feature set is aligned with the **2.6 branches**
  of the Java (`MilvusClientV2`), C++ (`MilvusClientV2`) and PyMilvus (`MilvusClient`) SDKs (see §4).
- **No conflicts**: V1 and V2 can be referenced together in one project without type ambiguity.

## 2. Architecture

### 2.1 Packages, namespaces, and assemblies

| Item | V1 (existing) | V2 (new) |
|---|---|---|
| NuGet package | `Milvus.Client` | `Milvus.Client.V2` |
| Assembly | `Milvus.Client` | `Milvus.Client.V2` |
| Namespace | `Milvus.Client` | `Milvus.Client.V2` (+ sub-namespaces) |
| Facade | `MilvusClient` | `MilvusClientV2` |
| Target frameworks | `net8.0;netstandard2.0;net462` | `net8.0;netstandard2.0;net462` |

- Both packages are published independently; `Milvus.Client.V2` has **no dependency** on
  `Milvus.Client`.
- Both generate gRPC code from the shared `Milvus.Client/Protos` submodule. The generated classes
  are `internal` and use the proto-declared `Milvus.Client.Grpc` namespace; being internal, they can
  never clash between the two assemblies even when both are referenced.
- Shared package version management is via `Directory.Packages.props` (central package management).
- Strict code quality is enforced by `Directory.Build.props`:
  `TreatWarningsAsErrors`, `AnalysisMode=All`, `Nullable=enable`, `ImplicitUsings=enable`.

### 2.2 Project layout

```
Milvus.Client.V2/
├── Milvus.Client.V2.csproj
│
├── MilvusClientV2.cs                 # Facade core (partial): ctor/ConnectConfig, channel, ConnectAsync
│                                     #   (+ lazy fallback), InvokeAsync, retry, Health/GetVersion/Dispose (§4.1)
├── MilvusClientV2.Collection.cs      # Collection domain facade  (§4.2)
├── MilvusClientV2.Index.cs           # Index domain facade       (§4.3)
├── MilvusClientV2.Dml.cs             # DML facade                (§4.5)
├── MilvusClientV2.Dql.cs             # DQL facade (incl. iterators)(§4.6)
├── MilvusClientV2.Partition.cs       # Partition domain facade   (§4.7)
├── MilvusClientV2.Database.cs        # Database domain facade    (§4.8)
├── MilvusClientV2.Alias.cs           # Alias domain facade       (§4.9)
├── MilvusClientV2.Rbac.cs            # RBAC facade               (§4.10)
├── MilvusClientV2.ResourceGroup.cs   # Resource group facade     (§4.11)
├── MilvusClientV2.Utility.cs         # Utility facade            (§4.12)
├── MilvusClientV2.BulkImport.cs      # BulkImport facade (REST)  (§4.13, planned)
│
├── MilvusErrorCode.cs                # Public error codes
├── MilvusException.cs                # Public exception type
│
├── Request/<Domain>/*Req.cs          # Per-operation request DTOs + ToGrpc*() conversion
│   # Domains mirror the facade areas: Collection, Index, Dml, Dql, Partition, Database,
│   #   Alias, Rbac, ResourceGroup, Utility, BulkImport
├── Response/<Domain>/*Resp.cs        # Per-operation typed responses + FromGrpc() factories
│
├── Types/                            # Public shared types (data model)
│   ├── ConnectConfig.cs              # Connection parameters (mirrors C++ ConnectParam / Java ConnectConfig)
│   ├── RetryConfig.cs                # Retry parameters (§5.2)
│   ├── DataType.cs  ConsistencyLevel.cs  FunctionType.cs
│   ├── IndexType.cs  SimilarityMetricType.cs      # planned (§4.14)
│   ├── FieldSchema.cs  CollectionSchema.cs  FunctionSchema.cs
│   ├── FieldData.cs  MilvusSparseVector.cs  MilvusHealthState.cs
│   └── Float16Utils.cs              # FP16/BFloat16 conversions (§3.5.2)
│
└── Utils/                            # Internal helpers
    ├── Verify.cs  Constants.cs  MilvusTimestampUtils.cs
    ├── Logging.cs  CompilerAttributes.cs  NullableAttributes.cs
    ├── CollectionCacheKey.cs  CollectionTsCache.cs  SchemaCache.cs  RetryPolicy.cs
    └── RestfulClient.cs              # REST client for BulkImport (§4.13, planned)
```

> The facade is a `partial class`; each feature area (§4) contributes one `MilvusClientV2.<Area>.cs`
> file (see §3.3 for the full mapping).

Test project layout (`Milvus.Client.V2.Tests/`, layered by purpose and grouped by area, see §7):

```
Milvus.Client.V2.Tests/
├── Unit/                 # Pure logic: DTO->proto conversion, validation, timestamp utils
├── Integration/          # Facade connectivity via in-process TestServer gRPC mock (no Docker)
├── System/               # End-to-end against a real Milvus container (milvus_container.py)
├── MilvusV2Fixture.cs    # AssemblyFixture owning the test container
└── milvus_container.py   # Starts/stops Milvus + MinIO for System tests
```

Tests are tagged with xunit traits (`Category=Unit|Integration|System`) so CI can split runs:

```bash
dotnet test --filter "Category=Unit|Category=Integration"   # fast, per-PR
dotnet test --filter "Category=System"                       # slow, nightly/matrix
```

## 3. Design Patterns

### 3.1 DTO pattern (per operation)

Every operation follows:

1. Caller builds an immutable-ish request DTO (`*Req`) via object initializer / constructor.
2. The facade method validates and calls the request's internal `ToGrpc*()` conversion to produce
   the protobuf request.
3. The facade invokes gRPC through a central `InvokeAsync` helper that checks `common.Status`,
   maps failures to `MilvusException`, and applies retry.
4. The protobuf response is wrapped by the response DTO's internal `FromGrpc()` factory.

```csharp
// Request DTO
public sealed class CreateCollectionReq
{
    public string CollectionName { get; set; } = "";
    public CollectionSchema? Schema { get; set; }
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Session;
    public int ShardsNum { get; set; } = 1;

    internal Grpc.CreateCollectionRequest ToGrpcCreateCollectionRequest() { ... }
}

// Facade method
public Task CreateCollectionAsync(CreateCollectionReq request, CancellationToken ct = default)
{
    Verify.NotNull(request);
    Grpc.CreateCollectionRequest grpcRequest = request.ToGrpcCreateCollectionRequest();
    return InvokeAsync(GrpcClient.CreateCollectionAsync, grpcRequest, ct);
}
```

**Response return rule** — return a typed response DTO only when the operation yields data;
otherwise return a bare `Task`:

| Operation kind | Facade return | Example |
|---|---|---|
| No result data | `Task` | `CreateCollectionAsync`, `DropCollectionAsync` |
| Returns data | `Task<TResp>` | `HasCollectionAsync` → `Task<HasCollectionResp>`, `ListCollectionsAsync` → `Task<ListCollectionsResp>` |

- A bare `Task` carries no data; success is a normal completion and failure is a thrown
  `MilvusException` (same exception-driven model as the Java SDK; the C++ SDK's `Status` return
  value is the C# equivalent of the exception, not of the response).
- Response DTOs are immutable: `private` constructor, internal `FromGrpc()` factory, read-only
  properties, so callers cannot construct or mutate them.

**Naming convention** — request/response DTOs use the `*Req` / `*Resp` suffix (e.g.
`CreateCollectionReq`, `DescribeCollectionResp`), for two reasons:

1. **Alignment with the Java V2 SDK**, which uses the same abbreviations
   (`CreateCollectionReq`, `DescribeCollectionResp`); the C++ SDK uses the full
   `*Request`/`*Response` forms.
2. **Avoiding type collisions with the generated proto messages**, which are named with the full
   form (`CreateCollectionRequest`, `CreateCollectionResponse`) in the `Milvus.Client.Grpc`
   namespace. The `Req`/`Resp` abbreviations keep the public DTOs distinct from the internal wire
   messages.

**Construction style — object initializers, not builders.** Request DTOs are mutable classes with
public settable properties, built with the C# **object initializer** syntax instead of the Java
builder pattern:

```csharp
var req = new CreateCollectionReq
{
    CollectionName = "book",
    Schema = new CollectionSchema
    {
        Fields =
        {
            new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
            FieldSchema.CreateFloatVector("embedding", dimension: 4)
        }
    }
};
```

- `new Req { Prop = value, ... }` is native C# (no extra builder classes), and collection properties
  use the nested collection-initializer form (`Fields = { ... }`).
- This expresses the same DTO pattern as the Java builder/C++ setter style, but in the idiomatic
  .NET way (as with `HttpClient`/`JsonSerializerOptions`/EF entities), so no
  `CreateCollectionReqBuilder`-style helper types are needed.

### 3.2 Connection configuration (`ConnectConfig`)

Instead of many constructor overloads, the facade accepts a single configuration object, mirroring
`ConnectParam` (C++) / `ConnectConfig` (Java). The gRPC channel is created **inside** the
constructor from the config.

```csharp
public sealed class ConnectConfig
{
    public string Uri { get; set; } = "";              // e.g. "localhost:19530" or "https://host:19530"
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ApiKey { get; set; }
    public string? Database { get; set; }
    public TimeSpan? ConnectTimeout { get; set; }
    public ILoggerFactory? LoggerFactory { get; set; }
    public GrpcChannelOptions? ChannelOptions { get; set; }   // advanced/tests
    public RetryConfig? Retry { get; set; }                   // planned
}
```

**Connection model — explicit `ConnectAsync` with lazy fallback.** Unlike the Java/C++ SDKs (which
connect eagerly in the constructor), the C# constructor is lightweight and performs **no network
I/O** (consistent with AWS/Azure/.NET convention). Connection is established via
`MilvusClientV2.ConnectAsync`, which sends the `MilvusService.Connect` RPC registering the SDK info
(type/version/user/host/time):

- **Recommended**: call `await client.ConnectAsync(ct)` before other APIs so connection/
  authentication failures surface up front.
- **Lazy fallback**: if `ConnectAsync` is not called, the **first** API call connects lazily
  (each facade method begins with an internal `EnsureConnectedAsync`).
- `ConnectAsync` is idempotent and retryable after failure.

```csharp
var client = new MilvusClientV2(new ConnectConfig { Uri = "localhost:19530" });  // lightweight, no I/O
await client.ConnectAsync();                    // optional; recommended for fail-fast
await client.CreateCollectionAsync(req);        // connects lazily if ConnectAsync was skipped
```

### 3.3 Facade as a partial class

`MilvusClientV2` is a `partial class`; each feature domain lives in its own file, similar to how
the C++ SDK splits implementations. The facade files map one-to-one onto the Feature Scope areas
(§4):

| Facade file | Feature Scope area | Sample methods |
|---|---|---|
| `MilvusClientV2.cs` | §4.1 Connection / utility (infrastructure) | ctor, `HealthAsync`, `GetServerVersionAsync`, `Dispose`, `InvokeAsync` |
| `MilvusClientV2.Collection.cs` | §4.2 Collection & schema | `CreateCollectionAsync`, `HasCollectionAsync`, `DescribeCollectionAsync` |
| `MilvusClientV2.Index.cs` | §4.3 Index | `CreateIndexAsync`, `DropIndexAsync`, `DescribeIndexAsync` |
| `MilvusClientV2.Dml.cs` | §4.5 DML | `InsertAsync`, `UpsertAsync`, `DeleteAsync` |
| `MilvusClientV2.Dql.cs` | §4.6 DQL | `SearchAsync`, `QueryAsync`, `HybridSearchAsync`, iterators |
| `MilvusClientV2.Partition.cs` | §4.7 Partition | `CreatePartitionAsync`, `LoadPartitionsAsync` |
| `MilvusClientV2.Database.cs` | §4.8 Database | `CreateDatabaseAsync`, `ListDatabasesAsync` |
| `MilvusClientV2.Alias.cs` | §4.9 Alias | `CreateAliasAsync`, `ListAliasesAsync` |
| `MilvusClientV2.Rbac.cs` | §4.10 RBAC | `CreateUserAsync`, `GrantPrivilegeAsync`, privilege groups |
| `MilvusClientV2.ResourceGroup.cs` | §4.11 Resource group | `CreateResourceGroupAsync`, `TransferReplicaAsync` |
| `MilvusClientV2.Utility.cs` | §4.12 Utility | `FlushAsync`, `CompactAsync`, `RunAnalyzerAsync` |
| `MilvusClientV2.BulkImport.cs` | §4.13 BulkImport | `ImportAsync`, `GetImportProgressAsync`, `ListImportJobsAsync` |

Each facade file only **orchestrates** (verify → `ToGrpc*()` → `InvokeAsync` → `FromGrpc()`); the
actual request/response conversion lives in the `Request/*.cs` and `Response/*.cs` classes (§3.1).
The cache/retry mechanisms hook into the facade methods (§5).

### 3.4 Central invocation & error handling

```csharp
internal async Task<TResponse> InvokeAsync<TRequest, TResponse>(
    Func<TRequest, CallOptions, AsyncUnaryCall<TResponse>> func,
    TRequest request,
    Func<TResponse, Grpc.Status> getStatus,
    CancellationToken ct, ...)
```

- Checks `common.Status`; non-success maps to `MilvusException(ErrorCode, Reason)`.
- Transport-level `RpcException` (network/auth) is wrapped into `MilvusException` for consistent
  error surface.
- Retry policy (see §5.2) is applied here.

### 3.5 Public API must not expose proto / gRPC types

Users must never see the generated `Milvus.Client.Grpc` (proto) types. All proto messages,
services and status types are **internal** to the `Milvus.Client.V2` assembly; the public surface
consists only of SDK DTOs (`*Req`/`*Resp`), configuration, enums and `MilvusException`.

Enforcement:

- **Proto generation is `Access="internal"`** (`GrpcServices="Both"` in the csproj), so the
  generated `Milvus.Client.Grpc.*` classes are not visible to consumers.
- **Public facade signatures contain no `Grpc.*` references** — they take a `*Req` and return a
  `*Resp` (or `Task`/`MilvusHealthState`). The `Grpc.MilvusService.MilvusServiceClient` and
  `InvokeAsync` are internal.
- **All proto↔DTO conversion is internal** (`Request.ToGrpc*()`, `Response.FromGrpc()`), so proto
  types never leak through `MilvusException` (which exposes only `MilvusErrorCode` + `Reason`).
- `InternalsVisibleTo` is granted **only** to the test assembly.

This keeps the public contract stable and independent of the underlying gRPC/proto version.

#### 3.5.1 Class visibility model

C# access modifiers control exactly what a consumer can see. The rule is: **the public surface is
the facade + its DTOs only; everything that supports them is internal**.

| Visibility | Contents |
|---|---|
| **public** | `MilvusClientV2` (facade), `*Req` / `*Resp` DTOs, `Types` (config & data model: `ConnectConfig`, `RetryConfig`, `DataType`, `IndexType`, `SimilarityMetricType`, `FieldSchema`, `CollectionSchema`, `FieldSchema`, `MilvusHealthState`, ...), `MilvusException`, `MilvusErrorCode`, `MilvusTimestampUtils`, and the FP16 utility |
| **internal** | `Milvus.Client.Grpc.*` (generated proto), `InvokeAsync`, the gRPC client, `ToGrpc*()` / `FromGrpc()`, `Utils` helpers (`Verify`, `Constants`, `LoggingExtensions`), caches (`CollectionCacheKey`, `CollectionTsCache`, `SchemaCache`), `RetryPolicy`, `RestfulClient` |
| `InternalsVisibleTo("Milvus.Client.V2.Tests")` | tests only |

`sealed` is used on **leaf** DTO classes (request/response types and concrete value types) so they
cannot be further inherited. **Abstract base types / interfaces are deliberately inheritable** —
e.g. `FieldData` (abstract, with `FieldData<T>` and concrete `*FieldData` leafs), `AnnSearchRequest`
(abstract, with `VectorAnnSearchRequest<T>`/`SparseVectorAnnSearchRequest<T>`/`TextAnnSearchRequest`),
and `IReranker` (interface, with `RrfReranker`/`WeightedReranker`). This mirrors the Java/C++
designs, where the base types form an inheritance hierarchy and only the concrete leafs are final/
immutable. The facade `MilvusClientV2` itself is `sealed`.

#### 3.5.2 FP16 / BFloat16 conversion utility

C++ ships `utils/FP16.h` and Java ships `common/utils/Float16Utils.java`; C# V2 provides an
equivalent public helper so callers can convert between `float` and half/bfloat16 bit patterns when
feeding `Float16Vector` / `BFloat16Vector` data:

```csharp
namespace Milvus.Client.V2;

public static class Float16Utils
{
    // scalar conversions
    public static ushort FloatToFp16(float value);       // F32 -> F16 bits (uint16)
    public static float Fp16ToFloat(ushort bits);         // F16 bits -> F32
    public static ushort FloatToBf16(float value);        // F32 -> BF16 bits
    public static float Bf16ToFloat(ushort bits);         // BF16 bits -> F32

    // vector conversions
    public static ushort[] F32VectorToFp16(IReadOnlyList<float> values);
    public static float[] Fp16VectorToF32(IReadOnlyList<ushort> bits);
    public static ushort[] F32VectorToBf16(IReadOnlyList<float> values);
    public static float[] Bf16VectorToF32(IReadOnlyList<ushort> bits);
}
```

- On **net8.0** this maps to `BitConverter.SingleToHalf` / `BitConverter.HalfToUInt16Bits` and a
  bfloat16 truncation; on **netstandard2.0 / net462** (no native `Half`) it uses the same bit
  manipulation the V1 SDK already performs (`#if NET8_0_OR_GREATER` guards).
- Where the C# type `Half` is used in `FieldData` it is likewise guarded with
  `#if NET8_0_OR_GREATER` (see V1 `Float16VectorFieldData`).

#### 3.5.3 `FieldData` type system

`FieldData` is an abstract base with a generic `FieldData<TData>` and concrete sealed leafs,
mirroring V1. Every insert/query row is a list of `FieldData`, one per field:

```csharp
public abstract class FieldData { ... }
public class FieldData<TData> : FieldData            // generic scalar leaf, e.g. FieldData<long>
public sealed class FloatVectorFieldData : FieldData<ReadOnlyMemory<float>>
public sealed class Float16VectorFieldData : FieldData<ReadOnlyMemory<Half>>        // #if NET8_0_OR_GREATER
public sealed class BFloat16VectorFieldData : FieldData<ReadOnlyMemory<ushort>>     // planned (net8.0: Half)
public sealed class BinaryVectorFieldData : FieldData<ReadOnlyMemory<byte>>
public sealed class SparseFloatVectorFieldData : FieldData<MilvusSparseVector<float>>
public sealed class ArrayFieldData<TElementData> : FieldData<IReadOnlyList<TElementData>?>
public sealed class ByteStringFieldData : FieldData                                  // JSON / blob rows
```

| Milvus `DataType` | C# `FieldData` representation |
|---|---|
| `Bool` / `Int8` / `Int16` / `Int32` / `Int64` / `Float` / `Double` / `VarChar` | `FieldData<T>` with `IReadOnlyList<T>` (`bool`, `sbyte`, `short`, `int`, `long`, `float`, `double`, `string`) |
| `FloatVector` | `FieldData<ReadOnlyMemory<float>>` / `FloatVectorFieldData` |
| `Float16Vector` | `FieldData<ReadOnlyMemory<Half>>` / `Float16VectorFieldData` (net8.0; bit-pattern `ushort` elsewhere) |
| `BFloat16Vector` | `FieldData<ReadOnlyMemory<ushort>>` / `BFloat16VectorFieldData` (planned; net8.0 may use a `Half`-like wrapper) |
| `BinaryVector` | `FieldData<ReadOnlyMemory<byte>>` / `BinaryVectorFieldData` |
| `SparseFloatVector` | `FieldData<MilvusSparseVector<float>>` / `SparseFloatVectorFieldData` |
| `Array<TElement>` | `FieldData<IReadOnlyList<TElement>?>` / `ArrayFieldData<TElement>` |
| `JSON` / dynamic rows | `ByteStringFieldData` or `FieldData<string>` |

Convenience factories mirror V1: `FieldData.Create(name, IReadOnlyList<T>)`,
`FieldData.CreateVarChar(...)`, `FieldData.CreateFloatVector(...)`,
`FieldData.CreateBinaryVectors(...)`, `FieldData.CreateJson(...)`, `FieldData.CreateSparseFloatVector(...)`.

## 4. Feature Scope (implementation roadmap)

> **Goal**: the V2 first-phase feature set is aligned with the **2.6 branches** of the Java
> (`MilvusClientV2`), C++ (`MilvusClientV2`) and PyMilvus (`MilvusClient`) SDKs. Features in the
> tables below follow those branches; anything added after 2.6 is a follow-up iteration (see the
> note at the end of this section).
>
> "Implemented" = present in the current codebase; "Planned" = on the roadmap.
>
> Table columns: **name** (facade method), **request** (request DTO class), **response** (response
> DTO class; omitted when the operation returns no data, per the §3.1 Response return rule),
> **description** (functional description).
>
> **Field-level source of truth**: this document specifies the C#-specific design (structure,
> mechanisms, type mappings). The exact per-field members of each `*Req`/`*Resp` are **not** listed
> here — the Java/C++ 2.6 branch request/response classes are the authoritative field definitions
> (this document only fixes names, layout and C#-specific semantics). Implement each DTO by
> translating the corresponding Java `io.milvus.v2.service.<area>.request/response` or C++
> `src/include/milvus/request|response/<area>` class into a C# object-initializer style class.

### 4.1 Connection / utility

| name | request | response | description |
|---|---|---|---|
| `MilvusClientV2` (ctor) | `ConnectConfig` | — | Creates the gRPC channel and stub (no network I/O) |
| `ConnectAsync` | — | — | Sends the `Connect` RPC registering client info; recommended before other APIs, but the first API call connects lazily if skipped |
| `HealthAsync` | — | `MilvusHealthState` | Checks the health of the Milvus server |
| `GetServerVersionAsync` | `GetServerVersionReq` | `GetServerVersionResp` | Gets the Milvus server version; `Detail=true` also returns build info |
| `Dispose` | — | — | Releases the gRPC channel and other resources |

### 4.2 Collection & schema (21)

| name | request | response | description |
|---|---|---|---|
| `CreateCollectionAsync` | `CreateCollectionReq` | — | Creates a collection with the given schema |
| `DropCollectionAsync` | `DropCollectionReq` | — | Drops a collection |
| `TruncateCollectionAsync` | `TruncateCollectionReq` | — | Removes all entities of a collection |
| `HasCollectionAsync` | `HasCollectionReq` | `HasCollectionResp` | Checks whether a collection exists |
| `DescribeCollectionAsync` | `DescribeCollectionReq` | `DescribeCollectionResp` | Describes the schema of a collection (SchemaCache hook) |
| `ListCollectionsAsync` | `ListCollectionsReq` | `ListCollectionsResp` | Lists all collections in the database |
| `GetCollectionStatsAsync` | `GetCollectionStatsReq` | `GetCollectionStatsResp` | Gets collection statistics, e.g. row count |
| `RenameCollectionAsync` | `RenameCollectionReq` | — | Renames a collection (updates caches via `Move`) |
| `LoadCollectionAsync` | `LoadCollectionReq` | — | Loads a collection into memory |
| `ReleaseCollectionAsync` | `ReleaseCollectionReq` | — | Releases a loaded collection from memory |
| `GetLoadStateAsync` | `GetLoadStateReq` | `GetLoadStateResp` | Gets the load state of a collection |
| `RefreshLoadAsync` | `RefreshLoadReq` | — | Refreshes the loaded data of a collection |
| `AlterCollectionPropertiesAsync` | `AlterCollectionPropertiesReq` | — | Alters collection properties |
| `DropCollectionPropertiesAsync` | `DropCollectionPropertiesReq` | — | Drops collection properties |
| `AddCollectionFieldAsync` | `AddCollectionFieldReq` | — | Adds a field to the collection schema |
| `AlterCollectionFieldAsync` | `AlterCollectionFieldReq` | — | Alters an existing field of the schema |
| `DropCollectionFieldPropertiesAsync` | `DropCollectionFieldPropertiesReq` | — | Drops properties of a schema field |
| `AddCollectionFunctionAsync` | `AddCollectionFunctionReq` | — | Adds a function (e.g. BM25) to the schema |
| `AlterCollectionFunctionAsync` | `AlterCollectionFunctionReq` | — | Alters an existing schema function |
| `DropCollectionFunctionAsync` | `DropCollectionFunctionReq` | — | Drops a schema function |
| `DescribeReplicasAsync` | `DescribeReplicasReq` | `DescribeReplicasResp` | Describes the replicas of a collection |

### 4.3 Index (6)

| name | request | response | description |
|---|---|---|---|
| `CreateIndexAsync` | `CreateIndexReq` | — | Creates an index on a field |
| `DropIndexAsync` | `DropIndexReq` | — | Drops an index |
| `DescribeIndexAsync` | `DescribeIndexReq` | `DescribeIndexResp` | Describes an index |
| `ListIndexesAsync` | `ListIndexesReq` | `ListIndexesResp` | Lists the index names of a collection |
| `AlterIndexPropertiesAsync` | `AlterIndexPropertiesReq` | — | Alters index properties |
| `DropIndexPropertiesAsync` | `DropIndexPropertiesReq` | — | Drops index properties |

### 4.5 DML (3)

| name | request | response | description |
|---|---|---|---|
| `InsertAsync` | `InsertReq` | `InsertResp` | Inserts rows; updates `CollectionTsCache` (§5.1.2) |
| `UpsertAsync` | `UpsertReq` | `UpsertResp` | Inserts or updates rows; updates `CollectionTsCache` |
| `DeleteAsync` | `DeleteReq` | `DeleteResp` | Deletes rows by expression; updates `CollectionTsCache` |

### 4.6 DQL (6)

| name | request | response | description |
|---|---|---|---|
| `GetAsync` | `GetReq` | `GetResp` | Fetches rows by primary key |
| `QueryAsync` | `QueryReq` | `QueryResp` | Queries rows by expression (reads `CollectionTsCache` for Session consistency) |
| `SearchAsync` | `SearchReq` | `SearchResp` | Performs vector similarity search (reads `CollectionTsCache` for Session consistency) |
| `HybridSearchAsync` | `HybridSearchReq` | `SearchResp` | Performs hybrid search over multiple ANN requests with reranking |
| `QueryIteratorAsync` | `QueryIteratorReq` | `QueryIterator` | Iterates over query results in batches |
| `SearchIteratorAsync` | `SearchIteratorReq` | `SearchIterator` | Iterates over search results in batches |

**Iterators** (`QueryIterator` / `SearchIterator`): returned by the iterator APIs and consumed with
`await foreach` (mirroring V1 `QueryWithIteratorAsync`). They lazily page over the server in
`batchSize`-sized batches (default 1000, range 1–16384):

```csharp
await foreach (IReadOnlyList<FieldData> batch in collection.QueryIteratorAsync(expression: "id > 0"))
{
    // each batch is a page of rows
}
```

- Implemented over `IAsyncEnumerable<IReadOnlyList<FieldData>>` with
  `[EnumeratorCancellation]` on the `CancellationToken`.
- The server-side iterator is driven via the query/search request params (`batch_size`, `limit`,
  `offset`) and a `describe` call to resolve the primary key for cursor advance; `offset` is not
  supported with an iterator (throws), matching V1.
- `QueryIteratorReq`/`SearchIteratorReq` carry the same inputs as `QueryReq`/`SearchReq` plus
  `batchSize`; `QueryIterator`/`SearchIterator` are the concrete iterator types.

**Rerankers** (for `HybridSearchAsync`): `IReranker` implementations:
`RrfReranker(float k)` (default k = 60) and `WeightedReranker(params float[] weights)` (one weight per
ANN request) — same shape as Java `RrfReranker`/`WeightedReranker` (field definitions per the §4
field-level source of truth).

### 4.7 Partition (7)

| name | request | response | description |
|---|---|---|---|
| `CreatePartitionAsync` | `CreatePartitionReq` | — | Creates a partition |
| `DropPartitionAsync` | `DropPartitionReq` | — | Drops a partition |
| `HasPartitionAsync` | `HasPartitionReq` | `HasPartitionResp` | Checks whether a partition exists |
| `ListPartitionsAsync` | `ListPartitionsReq` | `ListPartitionsResp` | Lists the partitions of a collection |
| `GetPartitionStatsAsync` | `GetPartitionStatsReq` | `GetPartitionStatsResp` | Gets partition statistics |
| `LoadPartitionsAsync` | `LoadPartitionsReq` | — | Loads specific partitions into memory |
| `ReleasePartitionsAsync` | `ReleasePartitionsReq` | — | Releases specific partitions from memory |

### 4.8 Database (6)

| name | request | response | description |
|---|---|---|---|
| `CreateDatabaseAsync` | `CreateDatabaseReq` | — | Creates a database |
| `DropDatabaseAsync` | `DropDatabaseReq` | — | Drops a database |
| `ListDatabasesAsync` | `ListDatabasesReq` | `ListDatabasesResp` | Lists all databases |
| `DescribeDatabaseAsync` | `DescribeDatabaseReq` | `DescribeDatabaseResp` | Describes a database |
| `AlterDatabasePropertiesAsync` | `AlterDatabasePropertiesReq` | — | Alters database properties |
| `DropDatabasePropertiesAsync` | `DropDatabasePropertiesReq` | — | Drops database properties |

### 4.9 Alias (5)

| name | request | response | description |
|---|---|---|---|
| `CreateAliasAsync` | `CreateAliasReq` | — | Creates an alias for a collection (updates caches via `Copy`) |
| `DropAliasAsync` | `DropAliasReq` | — | Drops an alias |
| `AlterAliasAsync` | `AlterAliasReq` | — | Alters an alias to point to another collection |
| `ListAliasesAsync` | `ListAliasesReq` | `ListAliasesResp` | Lists the aliases |
| `DescribeAliasAsync` | `DescribeAliasReq` | `DescribeAliasResp` | Describes an alias |

### 4.10 RBAC (22)

Users and roles:

| name | request | response | description |
|---|---|---|---|
| `CreateUserAsync` | `CreateUserReq` | — | Creates a user |
| `DropUserAsync` | `DropUserReq` | — | Drops a user |
| `UpdatePasswordAsync` | `UpdatePasswordReq` | — | Updates a user's password |
| `UpdateUserAsync` | `UpdateUserReq` | — | Updates a user's remark |
| `ListUsersAsync` | `ListUsersReq` | `ListUsersResp` | Lists all users |
| `DescribeUserAsync` | `DescribeUserReq` | `DescribeUserResp` | Describes a user and its roles |
| `CreateRoleAsync` | `CreateRoleReq` | — | Creates a role |
| `DropRoleAsync` | `DropRoleReq` | — | Drops a role |
| `AlterRoleAsync` | `AlterRoleReq` | — | Alters a role's remark |
| `ListRolesAsync` | `ListRolesReq` | `ListRolesResp` | Lists all roles |
| `DescribeRoleAsync` | `DescribeRoleReq` | `DescribeRoleResp` | Describes a role and its users/grants |

Privileges and privilege groups:

| name | request | response | description |
|---|---|---|---|
| `GrantRoleAsync` | `GrantRoleReq` | — | Adds a user to a role |
| `RevokeRoleAsync` | `RevokeRoleReq` | — | Removes a user from a role |
| `GrantPrivilegeAsync` | `GrantPrivilegeReq` | — | Grants a privilege to a role (legacy) |
| `RevokePrivilegeAsync` | `RevokePrivilegeReq` | — | Revokes a privilege from a role (legacy) |
| `GrantPrivilegeV2Async` | `GrantPrivilegeReqV2` | — | Grants a privilege (v2 object/privilege model) |
| `RevokePrivilegeV2Async` | `RevokePrivilegeReqV2` | — | Revokes a privilege (v2 object/privilege model) |
| `CreatePrivilegeGroupAsync` | `CreatePrivilegeGroupReq` | — | Creates a privilege group |
| `DropPrivilegeGroupAsync` | `DropPrivilegeGroupReq` | — | Drops a privilege group |
| `ListPrivilegeGroupsAsync` | `ListPrivilegeGroupsReq` | `ListPrivilegeGroupsResp` | Lists all privilege groups |
| `AddPrivilegesToGroupAsync` | `AddPrivilegesToGroupReq` | — | Adds privileges to a privilege group |
| `RemovePrivilegesFromGroupAsync` | `RemovePrivilegesFromGroupReq` | — | Removes privileges from a privilege group |

### 4.11 Resource group (7)

| name | request | response | description |
|---|---|---|---|
| `CreateResourceGroupAsync` | `CreateResourceGroupReq` | — | Creates a resource group |
| `UpdateResourceGroupsAsync` | `UpdateResourceGroupsReq` | — | Updates resource groups |
| `DropResourceGroupAsync` | `DropResourceGroupReq` | — | Drops a resource group |
| `ListResourceGroupsAsync` | `ListResourceGroupsReq` | `ListResourceGroupsResp` | Lists all resource groups |
| `DescribeResourceGroupAsync` | `DescribeResourceGroupReq` | `DescribeResourceGroupResp` | Describes a resource group |
| `TransferNodeAsync` | `TransferNodeReq` | — | Transfers a query node between resource groups |
| `TransferReplicaAsync` | `TransferReplicaReq` | — | Transfers a replica between resource groups |

### 4.12 Utility (17)

| name | request | response | description |
|---|---|---|---|
| `FlushAsync` | `FlushReq` | `FlushResp` | Flushes a collection |
| `FlushAllAsync` | `FlushAllReq` | `FlushAllResp` | Flushes all collections |
| `GetFlushAllStateAsync` | `GetFlushAllStateReq` | `GetFlushAllStateResp` | Gets the flush-all state |
| `GetPersistentSegmentInfoAsync` | `GetPersistentSegmentInfoReq` | `GetPersistentSegmentInfoResp` | Gets persistent segment info |
| `GetQuerySegmentInfoAsync` | `GetQuerySegmentInfoReq` | `GetQuerySegmentInfoResp` | Gets loaded query-segment info |
| `CompactAsync` | `CompactReq` | `CompactResp` | Compacts a collection |
| `GetCompactionStateAsync` | `GetCompactionStateReq` | `GetCompactionStateResp` | Gets the compaction state |
| `GetCompactionPlansAsync` | `GetCompactionPlansReq` | `GetCompactionPlansResp` | Gets compaction plans |
| `DumpMessagesAsync` | `DumpMessagesReq` | `DumpMessagesResp` | Dumps CDC messages |
| `OptimizeAsync` | `OptimizeReq` | `OptimizeResp` | Optimizes a collection (index/compaction) |
| `GetReplicateInfoAsync` | `GetReplicateInfoReq` | `GetReplicateInfoResp` | Gets replication info |
| `GetReplicateConfigurationAsync` | `GetReplicateConfigurationReq` | `GetReplicateConfigurationResp` | Gets replication configuration |
| `UpdateReplicateConfigurationAsync` | `UpdateReplicateConfigurationReq` | `UpdateReplicateConfigurationResp` | Updates replication configuration |
| `RunAnalyzerAsync` | `RunAnalyzerReq` | `RunAnalyzerResp` | Runs the text analyzer on a string |
| `CheckHealthAsync` | — | `MilvusHealthState` | Checks server health |
| `GetServerVersionAsync` | `GetServerVersionReq` | `GetServerVersionResp` | Gets the server version with detail |
| `UseDatabaseAsync` | `UseDatabaseReq` | — | Switches the default database for this client |

### 4.13 BulkImport (3)

Milvus bulk import (data files → collection) is exposed as a **REST** API in the 2.6 SDKs (not
gRPC). C# V2 therefore needs a small REST client for this domain (the gRPC equivalents
`Import`/`GetImportState`/`ListImportTasks` exist in the proto but are not the 2.6 SDK surface).

| Java 2.6 `BulkImportUtils` | C++ 2.6 `BulkImport` | Request (C#) | Response (C#) | Description |
|---|---|---|---|---|
| `bulkImport(url, BaseImportRequest)` | `CreateImportJobs(url, collectionName, files, ...)` | `ImportReq` (collection, files, partition, options) | `ImportResp` (job id) | Creates an import job from data files |
| `getImportProgress(url, BaseDescribeImportRequest)` | `GetImportJobProgress(url, jobId, ...)` | `GetImportProgressReq` (job id) | `GetImportProgressResp` (progress/state) | Gets the progress of an import job |
| `listImportJobs(url, BaseListImportJobsRequest)` | `ListImportJobs(url, collectionName, ...)` | `ListImportJobsReq` (collection) | `ListImportJobsResp` (job list) | Lists import jobs of a collection |

> This is a **Planned** area. The C# REST client (`RestfulClient`) is a follow-up; the exact request
> fields follow the Java request classes (`BaseImportRequest` = `apiKey` + `options`,
> `BaseDescribeImportRequest` = `apiKey`, `BaseListImportJobsRequest` = `apiKey`) and the C++
> method parameters above. (C++ also has 2PC `Commit`/`Abort`, but the 2.6 parity scope is the 3
> APIs above.)

### 4.14 Supported enums (DataType / IndexType / MetricType)

The enum surface is aligned with the Java/C++ SDK 2.6 branches (numeric codes may differ between
SDKs; names are the parity unit). "In C# V2" marks whether the value is already present in the V2
`Types` enums (note: only `Types/DataType.cs` exists today; `Types/IndexType.cs` and
`Types/SimilarityMetricType.cs` are to be created, so their "Yes" marks refer to the planned parity
set).

#### DataType

| Java 2.6 | C++ 2.6 | In C# V2 (`Types/DataType`) |
|---|---|---|
| `None` / `Bool` / `Int8` / `Int16` / `Int32` / `Int64` | `UNKNOWN`/`BOOL`/`INT8`/`INT16`/`INT32`/`INT64` | Yes |
| `Float` / `Double` | `FLOAT` / `DOUBLE` | Yes |
| `VarChar` / `Array` / `JSON` | `VARCHAR` / `ARRAY` / `JSON` | Yes |
| `Geometry` (24) | `GEOMETRY` | **Missing — to add** |
| `Timestamptz` (26) | `TIMESTAMPTZ` | **Missing — to add** |
| `BinaryVector` / `FloatVector` / `Float16Vector` | `BINARY_VECTOR`/`FLOAT_VECTOR`/`FLOAT16_VECTOR` | Yes |
| `BFloat16Vector` (103) | `BFLOAT16_VECTOR` | **Missing — to add** |
| `SparseFloatVector` (104) | `SPARSE_FLOAT_VECTOR` | Yes |
| `Int8Vector` (105) | `INT8_VECTOR` | **Missing — to add** |
| `Struct` (201) | `STRUCT` | **Missing — to add** |

> `Text` (25) is post-2.6 (master only), not in the 2.6 scope. All the missing values above are
> already present in the pinned `milvus-proto` (`schema.proto`), so no proto bump is needed.

#### IndexType

| Java 2.6 | C++ 2.6 | In C# V2 (`Types/IndexType`) |
|---|---|---|
| `FLAT` / `IVF_FLAT` / `IVF_SQ8` / `IVF_PQ` | `FLAT`/`IVF_FLAT`/`IVF_SQ8`/`IVF_PQ` | Yes |
| `HNSW` | `HNSW` | Yes (as `Hnsw`) |
| `HNSW_SQ` / `HNSW_PQ` / `HNSW_PRQ` | `HNSW_SQ`/`HNSW_PQ`/`HNSW_PRQ` | **Missing — to add** (as `HnswSq`/`HnswPq`/`HnswPrq`) |
| `DISKANN` / `AUTOINDEX` / `SCANN` | `DISKANN`/`AUTOINDEX`/`SCANN` | Yes |
| `GPU_IVF_FLAT`/`GPU_IVF_PQ`/`GPU_BRUTE_FORCE`/`GPU_CAGRA` | same | Yes (as `GpuIvfFlat`/`GpuIvfPq`/`GpuBruteForce`/`GpuCagra`) |
| `BIN_FLAT` / `BIN_IVF_FLAT` | `BIN_FLAT`/`BIN_IVF_FLAT` | Yes (as `BinFlat`/`BinIvfFlat`) |
| `TRIE` | `TRIE` | Yes |
| `STL_SORT` / `INVERTED` / `BITMAP` | `STL_SORT`/`INVERTED`/`BITMAP` | Yes (`StlSort`/`Inverted`); `Bitmap` **to add** |
| `SPARSE_INVERTED_INDEX` / `SPARSE_WAND` | `SPARSE_INVERTED_INDEX`/`SPARSE_WAND` | Yes (`SparseInvertedIndex`); `SparseWand` **to add** |
| — | `IVF_RABITQ` / `AISAQ` / `MINHASH_LSH` / `NGRAM` / `RTREE` (C++-only) | Not in 2.6 parity scope |

> Note: the C# V1 `RhnswFlat`/`RhnswPq`/`RhnswSq` values belong to the separate **RAFT-HNSW**
> family and are **not** the same as `HNSW_SQ`/`HNSW_PQ`/`HNSW_PRQ` (standard-HNSW quantized
> variants); they must not be mapped to each other.

#### MetricType

| Java 2.6 (`IndexParam.MetricType`) | C++ 2.6 | In C# V2 (`Types/SimilarityMetricType`) |
|---|---|---|
| `L2` / `IP` / `COSINE` | `L2`/`IP`/`COSINE` | Yes |
| `HAMMING` / `JACCARD` / `MHJACCARD` | `HAMMING`/`JACCARD`/`MHJACCARD` | Yes (`Hamming`/`Jaccard`/`MhJaccard`) |
| `BM25` | `BM25` | Yes |
| `MAX_SIM` / `MAX_SIM_COSINE`/`MAX_SIM_IP`/`MAX_SIM_L2`/`MAX_SIM_JACCARD`/`MAX_SIM_HAMMING` | `MAX_SIM_COSINE`/`MAX_SIM_IP`/`MAX_SIM_L2`/`MAX_SIM_JACCARD`/`MAX_SIM_HAMMING` | **Missing — to add** |

> `DEFAULT` (C++) and `INVALID`/`None` (Java) are sentinel/unset values, not real metrics.

> Post-2.6 follow-ups (not in the 2.6 scope): snapshot/restore APIs, file resources, external
> collection refresh, `Text` data type, BM25 analyzer params, and schema field/function-field
> mutation APIs (`DropCollectionField`, `AddFunctionField`, `DropFunctionField`) that appear on
> SDK `master` branches.

## 5. Retry and Cache Mechanisms

Two important mechanisms shared across pymilvus / Java / C++ SDKs:

- **Retry**: automatically retry an API call on certain conditions.
- **Cache**: the SDK locally caches the collection schema (avoiding repeated
  `DescribeCollection`), and caches the per-collection DML last-timestamp (used by the
  `ConsistencyLevel.Session` mechanism).
  These are the `SchemaCache` / `CollectionTsCache` singletons in Java/C++, which this design
  mirrors.

### 5.1 Cache design

#### 5.1.1 Cache key

A collection is uniquely identified by `(endpoint, database, collection)`. The endpoint is
normalized (lower-case `host:port`), and an empty database defaults to `"default"`.

```csharp
internal readonly record struct CollectionCacheKey(string Endpoint, string Database, string Collection)
{
    public static CollectionCacheKey Create(string endpoint, string database, string collection);
}
```

Placed in `Utils/CollectionCacheKey.cs`. Being keyed by endpoint, one cache serves all client
instances (as in Java/C++).

#### 5.1.2 `CollectionTsCache` (DML last-timestamp)

Stores, per collection, the timestamp of the last local DML operation, used to build the
`guaranteeTimestamp` for `ConsistencyLevel.Session` queries.

**Purpose — the `Session` consistency mechanism.** Milvus queries accept a `guaranteeTimestamp`
that tells the query node to wait until all data up to that timestamp is visible before returning.
To implement *read-your-writes* within one session, the SDK:

1. after each local DML (`Insert`/`Upsert`/`Delete`), records the mutation timestamp returned by the
   server into `CollectionTsCache` (keyed by `endpoint`/`database`/`collection`);
2. on a later local `Search`/`Query` whose consistency level is `Session`, reads that timestamp and
   sends it as the request's `guaranteeTimestamp`, so the server blocks until the just-written rows
   are visible — the caller sees its own writes without needing `Strong` (which is more expensive).

The `guaranteeTimestamp` per consistency level (identical to Java `VectorUtils.getGuaranteeTimestamp`
and C++ `DqlUtils.DeduceGuaranteeTimestamp`):

| ConsistencyLevel | guaranteeTimestamp | Meaning |
|---|---|---|
| `Strong` | `0` | wait until **all** DML has finished (`GuaranteeStrongTs`) |
| `Session` | `CollectionTsCache.Get(...)`; `1` when absent | wait until **this client's** last DML is visible |
| `BoundedStaleness` | `2` | let the server decide the bounded staleness window |
| `Eventually` (and unspecified) | `1` | execute immediately (`GuaranteeEventuallyTs`) |

> `1` and `0` are the well-known constants `GuaranteeEventuallyTs` / `GuaranteeStrongTs`.

```csharp
public sealed class CollectionTsCache
{
    public static CollectionTsCache Instance { get; } = new();   // process-wide singleton

    public long Get(string endpoint, string database, string collection);   // 0 when absent
    public void Set(string endpoint, string database, string collection, long timestamp);
        // ignores 0; keeps the maximum (monotonic)
    public void Invalidate(string endpoint, string database, string collection);
    public void InvalidateDb(string endpoint, string database);
    public void Move(string endpoint, string srcDb, string srcCol, string dstDb, string dstCol);
        // rename: transfer latest ts to the new name, drop the old key
    public void Copy(string endpoint, string srcDb, string srcCol, string dstDb, string dstCol);
        // alias: transfer latest ts to the alias, keep the original
    public void Clear();
    public int Count { get; }
}
```

- Implementation: `ConcurrentDictionary<CollectionCacheKey, long>`; lock-free reads, `lock` for
  monotonic `Set` and the `Move`/`Copy`/`Invalidate*` writes.
- **Hook points**:
  - `InsertAsync` / `UpsertAsync` / `DeleteAsync` → `Set(...)` with the mutation timestamp (read side
    of the Session guarantee described above).
  - `SearchAsync` / `QueryAsync` with `ConsistencyLevel == Session` → `Get(...)` the cached ts and
    set it as the request's `guaranteeTimestamp` (`1` when the cache has no entry).
  - `RenameCollection` → `Move`; `CreateAlias`/`AlterAlias` → `Copy`; `DropCollection` →
    `Invalidate`.

#### 5.1.3 `SchemaCache` (collection schema)

Caches the result of `DescribeCollection` (the schema) so callers avoid repeated
`DescribeCollection` RPCs.

```csharp
public sealed class SchemaCache
{
    public static SchemaCache Instance { get; } = new();   // process-wide singleton

    // Returns the cached schema or loads it once via loader (single-flight: concurrent
    // requests for the same key trigger only one loader call).
    public ValueTask<DescribeCollectionResp> GetOrLoadAsync(
        CollectionCacheKey key, Func<CancellationToken, ValueTask<DescribeCollectionResp>> loader,
        CancellationToken ct);

    public void Invalidate(string endpoint, string database, string collection);
    public void InvalidateDb(string endpoint, string database);
    public void Clear();
}
```

- Implementation: `ConcurrentDictionary<CollectionCacheKey, Lazy<Task<...>>>` (or a per-key
  `SemaphoreSlim`) to guarantee single-flight loading.
- Only the **schema** is cached; load/segment state is always queried live to avoid staleness.
- **Hook points**:
  - `DescribeCollectionAsync` → `GetOrLoadAsync(...)`.
  - `DropCollection`, `AlterCollection*`, `RenameCollection` → `Invalidate` (or `Move`).
  - `CreateAlias`/`AlterAlias` → `Copy` (alias shares the schema).

#### 5.1.4 Where the cache is invoked (facade layer)

Both caches are read/written **from the facade orchestration methods**
(`MilvusClientV2.*.cs`), never from the conversion layer or from `InvokeAsync`:

| Layer | Touches the cache? | Reason |
|---|---|---|
| `InvokeAsync` (infrastructure) | No | too generic to know operation semantics |
| `Request.ToGrpc*` / `Response.FromGrpc` (conversion) | No | mapping only; avoids coupling to the cache singletons and keeps them unit-testable |
| Facade methods (orchestration) | **Yes** | the only layer that knows `collectionName`, the consistency level, and the operation kind |

Per-operation hook points:

```csharp
// SchemaCache — in DescribeCollectionAsync
public async Task<DescribeCollectionResp> DescribeCollectionAsync(DescribeCollectionReq request, CancellationToken ct)
{
    var key = CollectionCacheKey.Create(_endpoint, _database, request.CollectionName);
    return await SchemaCache.Instance.GetOrLoadAsync(key, ct => DescribeCollectionRpc(request, ct), ct);
}

// CollectionTsCache — write after DML
public async Task<InsertResp> InsertAsync(InsertReq request, CancellationToken ct)
{
    InsertResp resp = ...;
    CollectionTsCache.Instance.Set(_endpoint, _database, request.CollectionName, resp.Timestamp);
    return resp;
}

// CollectionTsCache — read for Session consistency in DQL
public async Task<SearchResp> SearchAsync(SearchReq request, CancellationToken ct)
{
    if (request.ConsistencyLevel == ConsistencyLevel.Session)
    {
        request.GuaranteeTimestamp = CollectionTsCache.Instance.Get(_endpoint, _database, request.CollectionName);
    }
    ...
}
```

To keep facade methods thin, the Session-consistency lookup can be factored into a private
helper (e.g. `EnsureSessionTimestamp(request, collectionName)`) reused by all DQL methods.

Cache maintenance on schema/alias changes also happens in the facade:
`RenameCollectionAsync` → `Move`, `CreateAliasAsync`/`AlterAliasAsync` → `Copy`,
`DropCollectionAsync` → `Invalidate` — the facade is the only place that knows the old/new
collection-name mapping.

#### 5.1.5 Testability

The singletons expose `Clear()`. The facade holds injectable cache references (defaulting to the
singletons) so tests can reset/isolate state without process restarts.

### 5.2 Retry design

#### 5.2.1 `RetryConfig`

Public, user-configurable, aligned with Java `RetryConfig` / C++ `RetryParam`:

```csharp
public sealed class RetryConfig
{
    public int MaxRetryTimes { get; set; } = 75;
    public TimeSpan InitialBackOff { get; set; } = TimeSpan.FromMilliseconds(10);
    public TimeSpan MaxBackOff { get; set; } = TimeSpan.FromSeconds(3);
    public int BackOffMultiplier { get; set; } = 3;
    public bool RetryOnRateLimit { get; set; } = true;
    public TimeSpan? MaxRetryTimeout { get; set; }   // null = no overall cap
}
```

Wired through `ConnectConfig.Retry` (default: a default `RetryConfig`).

#### 5.2.2 `RetryPolicy`

Internal helper applied inside `InvokeAsync`:

```csharp
internal static class RetryPolicy
{
    // Two-layer decision (see §5.2.4):
    //   grpc.RpcException  -> retry unless StatusCode is in the 7-code blacklist
    //   MilvusException    -> retry only when ErrorCode == RateLimit (8 / legacy 49)
    public static bool IsRetryable(Exception exception, RetryConfig config);

    // Exponential backoff: delay = min(MaxBackOff, InitialBackOff * multiplier^attempt)
    public static TimeSpan GetBackOff(RetryConfig config, int attempt);
}
```

- **Exponential backoff** capped at `MaxBackOff`, bounded by `MaxRetryTimeout`.
- The explicit `ConnectAsync` (and its lazy fallback) is not wrapped by the retry loop (fail-fast); it calls
  the gRPC `Connect` RPC directly.
- Only retryable errors (see the two-layer decision in §5.2.4) are retried; argument validation,
  authentication, and business errors fail immediately.

#### 5.2.3 How retry is interposed

Retry is applied **inside `InvokeAsync`** — the single gRPC entry point shared by every facade
method — so all operations get retry for free and the facade/conversion layers are unchanged:

```csharp
internal async Task<TResponse> InvokeAsync<TRequest, TResponse>(
    Func<TRequest, CallOptions, AsyncUnaryCall<TResponse>> func,
    TRequest request,
    Func<TResponse, Grpc.Status> getStatus,
    CancellationToken ct, ...)
{
    return await RetryPolicy.ExecuteAsync(
        async innerCt =>
        {
            TResponse response = await func(request, _callOptions.WithCancellationToken(innerCt)).ConfigureAwait(false);
            Grpc.Status status = getStatus(response);
            var code = (MilvusErrorCode)status.Code;
            if (code != MilvusErrorCode.Success)
            {
                throw new MilvusException(code, status.Reason);   // fed to RetryPolicy.IsRetryable
            }
            return response;
        },
        _retryConfig, ct).ConfigureAwait(false);
}
```

The `RetryPolicy.ExecuteAsync` loop:

```csharp
internal static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> call, RetryConfig config, CancellationToken ct)
{
    int attempt = 0;
    var deadline = config.MaxRetryTimeout is { } t ? DateTime.UtcNow + t : (DateTime?)null;

    while (true)
    {
        try
        {
            return await call(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsRetryable(ex, config) && attempt < config.MaxRetryTimes
                                   && (deadline is null || DateTime.UtcNow < deadline))
        {
            attempt++;
            await Task.Delay(GetBackOff(config, attempt), ct).ConfigureAwait(false);
        }
    }
}
```

Design points:

- **Retry granularity is the whole RPC**: each attempt re-sends the request via a fresh call. As in
  the Java/C++ SDKs, the whole operation is retried; callers of non-idempotent DML should keep this
  in mind.
- **The `Connect` RPC (`ConnectAsync` / lazy fallback) is not retried**: it calls the gRPC `Connect`
  directly (not through `InvokeAsync`), so connection/auth failures surface immediately.
- **Cancellation is honored**: the same `CancellationToken` flows through every attempt and through
  the backoff delay, so a user cancel aborts immediately.
- **Retry vs cache**: on success the cache is populated normally (the populating code in the facade
  runs after `InvokeAsync` returns); on final failure nothing is cached, keeping state consistent.

#### 5.2.4 Retryable error decision (gRPC code × Milvus server code)

The retry decision is based on **two layers of error codes**, matching the Java/C++/PyMilvus SDKs:

1. **gRPC transport error code** (`RpcException.StatusCode`) — an identical 7-code blacklist is
   shared by all three SDKs; those are **never** retried:

   | gRPC code | Retry? |
   |---|---|
   | `DeadlineExceeded`, `PermissionDenied`, `Unauthenticated`, `InvalidArgument`, `AlreadyExists`, `ResourceExhausted`, `Unimplemented` | **No** (fail immediately) |
   | every other code, notably `Unavailable`, `Cancelled`, `Unknown`, `Internal`, `NotFound`, ... | **Yes** |

2. **Milvus server error code** (`MilvusException.ErrorCode`, from `common.Status`) — only the
   **RateLimit** error is retried (when `RetryOnRateLimit`), with dual compatibility:
   - `ErrorCode.RateLimit == 8` (Milvus ≥ 2.3)
   - legacy `RateLimit == 49` (Milvus 2.2)
   - all other server error codes are **not** retried.

Cross-SDK verification (read from source):

| Behavior | Java `rpcUtils.retry` | C++ `RpcUtils::Retry` | PyMilvus `retry_on_rpc_failure` |
|---|---|---|---|
| gRPC blacklist (7 codes) | identical | identical | identical (`IGNORE_RETRY_CODES`) |
| `Unavailable` (transport) | **retries** | **does not retry** (server code 0 falls into `else if (!IsOk) return`) | **retries** |
| server RateLimit (8 / legacy 49) | retries | retries | retries |
| other server errors | no | no | no |
| global-cluster / connection recovery | `handleGlobalConnectionError` / `handleGlobalRoutingError` (topology refresh) | — | `_on_rpc_error` (e.g. REPLICATE_VIOLATION) |

**C# decision**: follow **PyMilvus** semantics — `Unavailable` and other non-blacklist gRPC codes
are retried, since PyMilvus (the reference client) and Java both retry them; only the 7 blacklisted
gRPC codes and non-RateLimit server errors fail immediately. The C# gRPC transport error surfaces
as `RpcException` and the server error as `MilvusException`, giving a natural two-exception mapping:

```csharp
public static bool IsRetryable(Exception exception, RetryConfig config)
    => exception switch
    {
        // Transport layer: retry unless the gRPC code is blacklisted (PyMilvus/Java semantics).
        RpcException { StatusCode: not (StatusCode.DeadlineExceeded or StatusCode.PermissionDenied
            or StatusCode.Unauthenticated or StatusCode.InvalidArgument or StatusCode.AlreadyExists
            or StatusCode.ResourceExhausted or StatusCode.Unimplemented) } => true,
        // Server layer: retry only on RateLimit (8) or legacy RateLimit (49) when enabled.
        MilvusException { ErrorCode: MilvusErrorCode.RateLimit } e => config.RetryOnRateLimit,
        MilvusException { ErrorCode: MilvusErrorCode.LegacyRateLimit } e => config.RetryOnRateLimit,
        _ => false
    };
```

> Note: `LegacyRateLimit (49)` may need to be added to `MilvusErrorCode` for 2.2-server
> compatibility (see §6).

## 6. Error model

- `MilvusErrorCode`: public enum mirroring server error codes (`Success=0`, `UnexpectedError=1`,
  `RateLimit=8`, `ForceDeny=9`, `CollectionNotFound=100`, `SegmentInfo=600`, `IndexNotFound=700`).
- `MilvusException`: wraps server `common.Status` (`ErrorCode` + `Reason`); also wraps transport
  `RpcException` into a consistent surface.
- Retryable codes feed the retry policy (§5.2).

## 7. Testing strategy

Tests are organized along two dimensions: **layer** (Unit / Integration / System) and **area**
(the Feature Scope domains of §4). Each layer is a top-level directory; within each layer, tests are
grouped by feature area (mirroring how the Java SDK tests under
`service/collection|index|rbac|...` and the C++ SDK tests under `test/st/cases/TestCollection.cpp`,
`TestDml.cpp`, ... are organized):

```
Milvus.Client.V2.Tests/
├── Unit/                        # Category=Unit
│   ├── Request/                 # per-request ToGrpc*() conversion (§4.x areas)
│   ├── Types/                   # enums, FieldSchema, cache logic
│   └── Utils/                   # timestamp utils, Verify
├── Integration/                 # Category=Integration
│   ├── Collection/  Dml/  Dql/  Index/  ...   # facade per area (§4.x)
│   ├── MockMilvusServer.cs
│   └── ClientConstructionTests.cs
└── System/                      # Category=System (real container)
    ├── CollectionTests.cs  DmlTests.cs  DqlTests.cs  ...
    └── MilvusV2Fixture.cs / MilvusTestContainer.cs
```

Layer | Purpose | Dependency | Speed
---|---|---|---
Unit | DTO→proto conversion, validation, timestamp utils, cache logic | none | ms
Integration | facade connectivity, request forwarding, error mapping, retry | in-process TestServer gRPC mock | s
System | correctness against a real Milvus server | Milvus container (milvus_container.py) | min

The layer × area split keeps CI flexible: `--filter "Category=Unit|Category=Integration"` runs the
fast layers per PR, `--filter "Category=System"` runs the e2e suite, and combining with the trait or
area name allows targeted regression per feature domain.

- The in-process mock server (`MockMilvusServer`) implements `MilvusServiceBase` (via
  `InternalsVisibleTo`) and lets tests control responses/failures, including retryable errors.
- System tests start a real Milvus (+ MinIO) container through `milvus_container.py`, with
  configurable ports (defaults: gRPC 29630, health 19191, MinIO 19100).

### 7.1 Code coverage

Coverage is collected with `coverlet` (the `coverlet.collector` package is already declared in
`Directory.Packages.props`) and reported via the cross-platform Cobertura format. Run:

```sh
dotnet test --collect "XPlat Code Coverage"
```

This produces one `coverage.cobertura.xml` under `TestResults/<run-id>/` per test project (the
`TestResults/` directory is ignored in `.gitignore`). For a readable HTML report, convert with
`reportgenerator`:

```sh
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" \
                -targetdir:coverage-report -reporttypes:Html
```

Coverage numbers are gated by two scoping rules so they reflect the SDK rather than test plumbing:

1. **`Threshold`** — a coverage minimum per scope (e.g. line/branch ≥ 80%) can be enforced in CI
   with `coverlet.msbuild` (a `Threshold` under the `XPlat Code Coverage` collector) so the build
   fails when coverage regresses.
2. **`ExcludeByAttribute`** / **`ExcludeByFilter`** — exclude generated code (proto stubs,
   `*.g.cs`) and the test projects themselves from the report, so the percentage measures
   `Milvus.Client.V2` source only.

When running only the fast layers (e.g. `--filter "Category=Unit|Category=Integration"`), coverage
reflects conversion/validation logic and mock-driven flows; the System layer needs the real
container and is therefore excluded from the per-PR coverage gate and measured in the nightly/matrix
run instead.

## 8. Examples and Tutorial

Like the Java and C++ SDKs, the C# SDK ships two companion artifacts for users: **Examples** and
**Tutorial**.

- **C++** organizes tutorials as numbered modules under `tutorial/` (`1_quickstart`,
  `2_collection`, `3_schema`, `4_index`, `5_dml`, `6_dql`, `7_database`, `8_rbac`, ...), each with its own
  `README.md`, source and build files, and examples under `examples/src/v2`.
- **Java** keeps examples in a Maven module `examples/` with sources under
  `examples/src/main/java/io/milvus/v2` and dedicated `bulk_writer` samples.

> **Reference mode differs between the two.** Examples reference the **source project**
> (`ProjectReference` to `Milvus.Client.V2.csproj`), so they build and run immediately from the repo.
> Tutorial references the **published NuGet package** (`PackageReference` to `Milvus.Client.V2`), so it
> is **not runnable until the V2 package is published** — it doubles as the release smoke test.

### 8.1 Examples (`examples/`)

Runnable, per-feature sample programs showing the DTO API in isolation. Built with
`ProjectReference` to `Milvus.Client.V2.csproj` (source project), so they run in the repo without any
packaging step.

```
examples/
├── MilvusExamples.sln            # solution over the example projects
├── README.md                     # prerequisites (Milvus server, connection), run instructions
├── src/
│   ├── ConnectionExample.cs      # ConnectConfig + ConnectAsync + HealthAsync/GetServerVersionAsync
│   ├── CollectionExample.cs      # create / describe / list / drop (+ SchemaCache)
│   ├── IndexExample.cs           # create / describe / list / drop index
│   ├── DmlExample.cs             # insert / upsert / delete
│   ├── DqlExample.cs             # query / search / hybrid search / iterators
│   ├── PartitionExample.cs  DatabaseExample.cs  AliasExample.cs
│   ├── RbacExample.cs  ResourceGroupExample.cs  UtilityExample.cs
│   └── BulkImportExample.cs
```

- Each example is a small `dotnet run`-able console program (`Program.cs` per example or a single
  solution with per-feature projects), mirroring the java/cpp per-feature sample style.
- Each example project references the source project:
  ```xml
  <ItemGroup><ProjectReference Include="..\..\Milvus.Client.V2\Milvus.Client.V2.csproj" /></ItemGroup>
  ```
- Examples connect to a configurable endpoint (env vars `MILVUS_URI` / `MILVUS_TOKEN`, defaulting to
  `localhost:19530`), following the ConnectionExample pattern.

### 8.2 Tutorial (`tutorial/`)

Step-by-step numbered modules that build on each other, mirroring the C++ `tutorial/N_<topic>`
layout. Tutorial is a single console solution that references the **published `Milvus.Client.V2`
package** (`PackageReference`), exactly as an end user would consume it — so it can only run after the
V2 package is published.

```
tutorial/
├── README.md                     # index of modules, ordering, prerequisites, "requires Milvus.Client.V2 package"
├── 1_quickstart/                 # connect -> create collection -> insert -> search -> drop
├── 2_collection/                 # schema & collection operations
├── 3_schema/                     # fields, data types, dynamic fields
├── 4_index/                      # index types & metric types
├── 5_dml/                        # insert / upsert / delete + Session consistency (ts cache)
├── 6_dql/                        # query / search / hybrid search / iterators
├── 7_database/                   # databases
├── 8_rbac/                       # users / roles / privileges
└── 9_bulk_import/                # bulk import
```

- Each module is a self-contained console project with its own `README.md` (what it demonstrates,
  how to run, expected output), following the C++ tutorial module style.
- Each module references the published package:
  ```xml
  <ItemGroup><PackageReference Include="Milvus.Client.V2" Version="..." /></ItemGroup>
  ```
- **Not runnable before the V2 package is published.** Once `Milvus.Client.V2` is on NuGet, run the
  tutorial as the release smoke test: it exercises the same public API an end user uses.
- Tutorial is intentionally simple and sequential; Examples are the reference for individual
  features. Content is written against the §4 feature scope.
