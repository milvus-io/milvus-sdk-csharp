# Tutorial 6: Query and search data

This standalone project demonstrates the main V2 data-query interfaces:

1. `QueryAsync` for scalar filtering and selected output fields.
2. `SearchAsync` for nearest-neighbor search on one vector field.
3. `HybridSearchAsync` for combining dense and sparse searches with weighted reranking.
4. `QueryIteratorAsync` for reading filtered entities in bounded batches.
5. `SearchIteratorAsync` for iterating a larger nearest-neighbor result set.

The program creates a collection with dense and sparse vector indexes, inserts a small data set,
loads the collection, runs every operation, and drops the collection afterward.

Results are read from the response's row accessors (`GetRow` returning a field dictionary, or
per-page `IReadOnlyList<FieldData>` for iterators) instead of materializing the full result at once.

## Prerequisites and run

- .NET SDK 8.0.x is installed.
- Milvus 2.6 or later is running and accessible.
- `Milvus.Client.V2` version `2.6.1` has been published to nuget.org.

Connection settings come from `MILVUS_URI` and `MILVUS_TOKEN`, defaulting to `localhost:19530` and
`root:Milvus`.

```bash
dotnet run --project tutorial/6_dql
```

The iterator examples deliberately use small batch and total limits so their pagination is easy to
observe.
