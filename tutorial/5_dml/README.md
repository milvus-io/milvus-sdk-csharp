# Tutorial 5: Insert, upsert, and delete data

This standalone project demonstrates the V2 data-manipulation interfaces:

1. Insert rows with row-oriented `RowsData` dictionaries.
2. Insert a batch using column-oriented `ColumnsData` (`FieldData` values).
3. Replace an existing entity with a full upsert.
4. Change selected fields with a partial upsert.
5. Delete entities by primary-key IDs.
6. Delete entities with a filter expression.
7. Query the remaining entities to verify the mutations.

An insert request must use either rows or columns, not both. All columns in one request must have
the same non-zero row count. Upserts require primary keys; partial upserts may omit unchanged
non-primary fields.

## Prerequisites and run

- .NET SDK 8.0.x is installed.
- Milvus 2.6 or later is running and accessible.
- `Milvus.Client.V2` version `2.6.0` has been published to nuget.org.

Connection settings come from `MILVUS_URI` and `MILVUS_TOKEN`, defaulting to `localhost:19530` and
`root:Milvus`.

```bash
dotnet run --project tutorial/5_dml
```

The program creates, loads, mutates, verifies, and drops a uniquely named collection.
