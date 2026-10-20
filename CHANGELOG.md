# Changelog

## milvus-sdk-csharp 2.6.1 (2026-10-20)

### Breaking Change

- Rename the V2 `SimilarityMetricType` enum to `MetricType`, aligning the public surface with the
  Java/C++/Rust V2 SDKs; the legacy enum is removed entirely. Consumers must recompile and use
  `MetricType` (the wire strings are unchanged).

### Feature

- Add embedding-list (Array-of-Struct vector sub-field, e.g. `clips[vector]`) search to
  `SearchIterator`
- Add `SearchIteratorReq.ExternalFilterFunc`, a client-side per-page row filter that returns the
  0-based row indices to keep (mirroring Java `externalFilterFunc` / C++ `SetExternalFilterFunc` /
  PyMilvus `external_filter_func`); filtered results are accumulated and yielded in full batches
- Expose per-collection shard counts (`ShardsNums`) on `ListCollectionsResp`, matching the Java
  per-collection `shardNum`

### Improvement

- `InsertAsync`/`UpsertAsync`/`GetAsync` short-circuit empty data/ids to an empty result without
  issuing any RPC, matching the Java SDK
- Add a `tutorial/` directory of standalone console projects that consume the published
  `Milvus.Client.V2` package, align the README with the other Milvus SDKs, and add `DEVELOPMENT.md`
- Align the mergify automation so the `ci-passed` label is dropped as soon as CI checks re-run

## milvus-sdk-csharp 2.6.0 (2026-10-16)

### Feature

- Introduce `MilvusClientV2`, a DTO request/response-style API aligned with Milvus 2.6 (per-operation
  `*Req` request + `*Resp` response, `ConnectAsync`), alongside the legacy `Milvus.Client` API in
  maintenance mode
- Connection: explicit `ConnectAsync`, database selection, connect timeout and RPC deadlines,
  gRPC keepalive, retry policy (rate-limit aware), and TLS via `ConnectConfig.ChannelOptions`
- Collection and schema: full and simple creation, index and schema creation, description,
  listing, statistics, replicas, rename, truncate, properties, add/alter/drop field, and
  collection functions
- Loading: load, refresh-load, release, load-state, synchronous waiting, and bounded operation
  timeouts
- Partition and alias: complete partition lifecycle and alias creation, retargeting, description,
  listing, and removal
- Database: selection and complete database lifecycle, description, and property management
- Index: creation with synchronous waiting, description, listing, removal, and property management
  for scalar and vector indexes
- DML: row- and column-based insert/upsert, partial upsert with per-field update operations
  (`FieldPartialUpdateOp`, incl. array append/remove), delete, nullable/default values,
  dynamic fields, and advanced field types
- DQL: get, query, search, hybrid search, primary-key ID queries/searches, grouping, full-text
  (BM25) and text match, reranking (weighted/RRF/function-score), highlighting, analyzer
  execution, and row-based result access (`GetRow` → field dictionaries)
- Iterators: query and search iterators as `IAsyncEnumerable` (lazy, paged)
- Utility: health and server-version inspection, metrics, flush/flush-all, segment listing,
  compaction, optimization, compaction state/plans
- CDC and replication: replication configuration, replication information, WAL message streaming
  (`DumpMessages`), checkpoints, and salvage positions
- Resource groups: lifecycle, configuration, description, listing, node transfer, and replica
  transfer operations
- RBAC: users, roles, role membership, privileges (v1 and v2 forms), privilege groups, password
  updates, and user/role alteration
- Support nullable and default-valued fields, dynamic fields, struct arrays, geometry, timestamps
  with time zones, arrays, JSON, and dense, binary, half-precision (fp16/bf16), sparse, and Int8
  vector types
- Add the Milvus 2.6 bulk-import REST interfaces as a `BulkImport` client built from
  `BulkImportConfig`: `BulkImportAsync`, `GetImportProgressAsync`, and `ListImportJobsAsync`

### Improvement

- Align schema-cache and session-timestamp-cache behavior with the Milvus C++ and Java SDKs,
  including alias normalization, collection rename handling, lifecycle invalidation, per-client
  in-flight loads, and cancellation safety
- Add typed V2 validation, conversion, server, timeout, cancellation, malformed-response, and
  retry-exhaustion errors (a uniform `MilvusException` with `MilvusErrorCode`)
- Expand V2 examples (per-feature runnable demos), docs, Trusted Publishing release automation,
  and Unit/Integration/System test coverage
- Harden the release workflow by validating tag ancestry and successful post-merge checks before
  publishing to nuget.org via Trusted Publishing (OIDC), then creating the GitHub release

## milvus-sdk-csharp 2.3.0-preview.1 (2024-03-20)

- Last preview release of the legacy `Milvus.Client` (V1) SDK.

## Legacy Milvus.Client (V1) history

- 2023-06-25 **2.2.1-beta.9**
  - Support Json field (grpc client only) (#19).
  - Support PartitionKey (grpc client only) (#19).
  - Support IsDynamic (grpc client only) (#19).
  - Support RBAC API (grpc client only) (#19).
  - Support Database (grpc client only) (#19).
  - Fix extraparam null issue (#19).
  - Fix autoid issue (#19).
- 2023-06-05 **2.2.1-alpha.8**
  - Fix float vector field dim split error
  - Add isdynamic and ispartitionkey to fieldType
- 2023-06-05 **2.2.1-alpha.7**
  - Add `WaitForLoadingProgressCollectionAsync` and `WaitForLoadingProgressCollectionValueAsync` methods.
- 2023-06-04 **2.2.1-alpha.6**
  - Fix field json convert error when query.
- 2023-06-04 **2.2.1-alpha.5**
  - Support restful client and grpc client.
  - Fix load partition error
  - Fix #14 about json format error when use restful client.
