# Tutorial 3: Define collection schemas

A schema defines the fields that each entity in a collection can contain. This standalone console
project downloads `Milvus.Client.V2` version `2.6.0` from nuget.org and demonstrates every usable V2
`DataType`.

The program creates three collections so the examples remain within common Milvus vector-field
limits:

1. Scalar and container fields, plus the vector field required by every Milvus collection.
2. Dense, binary, and half-precision vector fields.
3. Sparse and `Int8Vector` fields plus a struct-array field with scalar sub-fields.

It describes the collections to show the schemas returned by Milvus, then drops all tutorial
collections.

## Data types

| Category | `DataType` | Important schema settings |
|---|---|---|
| Boolean | `Bool` | Optional `Nullable` or `DefaultValue` |
| Integer | `Int8`, `Int16`, `Int32`, `Int64` | Primary keys may use `Int64` |
| Floating point | `Float`, `Double` | `Float` stores 32-bit values; `Double` stores 64-bit values |
| Text | `VarChar` | Set `MaxLength`; a primary key may use `VarChar` instead of `Int64` |
| Document | `Json` | Stores structured JSON values |
| Spatial | `Geometry` | Stores geometry in Well-Known Text form |
| Time | `Timestamptz` | Stores timestamp-with-time-zone values |
| Array | `Array` | Set `ElementDataType` and `MaxCapacity`; string arrays also need `MaxLength` |
| Struct array | `StructFieldSchema` | A positive `MaxCapacity` and sub-fields |
| Dense vector | `FloatVector`, `Float16Vector`, `BFloat16Vector`, `Int8Vector` | Set a positive `Dimension` |
| Sparse vector | `SparseFloatVector` | No dimension |

## Prerequisites and run

- .NET SDK 8.0.x is installed.
- Milvus 2.6 or later is running and accessible.
- `Milvus.Client.V2` version `2.6.0` has been published to nuget.org.

Connection settings come from `MILVUS_URI` and `MILVUS_TOKEN`, defaulting to `localhost:19530` and
`root:Milvus`.

```bash
dotnet run --project tutorial/3_schema
```

The program creates uniquely named collections, describes them, and drops them before exiting.
