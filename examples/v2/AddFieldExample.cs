using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Creates a collection, then adds a new field to its schema and inserts rows including the new field.
/// Mirrors cpp examples/src/v2/add_field.cpp and java AddFieldExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to evolve a schema after creation via
/// <c>AddCollectionFieldAsync</c> and then write the new column.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>AddCollectionFieldAsync</c>,
/// <c>InsertAsync</c>, <c>CreateIndexAsync</c>, <c>QueryAsync</c>, <c>DropCollectionFieldAsync</c>,
/// <c>AddFunctionFieldAsync</c>, <c>DropFunctionFieldAsync</c>, <c>DescribeCollectionAsync</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Rows returned: 4", then "Done.".</para>
/// </remarks>
public static class AddFieldExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "add_field_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusAddField_Add
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("name", maxLength: 128),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });

        // Rows written before the field exists carry no value for it (the "extra" column is absent).
        Console.WriteLine("Inserting 2 rows without the new field...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateVarChar("name", new[] { "a", "b" }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f })
                })
            ]
        });

        await client.AddCollectionFieldAsync(new AddCollectionFieldReq
        {
            CollectionName = collectionName,
            Field = FieldSchema.CreateVarchar("extra", maxLength: 64, nullable: true)
        });
        #endregion

        // Rows written after the schema change include the new field.
        Console.WriteLine("Inserting 2 rows with the new field...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 3, 4 }),
                FieldData.CreateVarChar("name", new[] { "c", "d" }),
                FieldData.CreateVarChar("extra", new[] { "x1", "x2" }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 1f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 0f })
                })
            ]
        });

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new Milvus.Client.V2.Types.IndexParam("vector", null, IndexType.Flat, MetricType.L2) ]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        // count(*) is a query *output field*, not an expression -- an empty filter with the count(*) output
        // makes the proxy return the row count. Confirms all four rows landed (2 pre-add + 2 post-add).
        Console.WriteLine("Counting rows...");
        QueryResp count = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Parameters = new QueryParameters
            {
                OutputFields = { "count(*)" },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        ExampleHelpers.PrintQueryRows(count);

        Console.WriteLine("Querying...");
        QueryResp results = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2, 3, 4]",
            Parameters = new QueryParameters { OutputFields = { "id", "name", "extra" } }
        });
        Console.WriteLine($"Rows returned: {results.FieldsData.FirstOrDefault()?.RowCount ?? 0}");
        ExampleHelpers.PrintQueryRows(results);

        #region Snippet:MilvusAddField_DropField
        // Drop a field from the schema.
        Console.WriteLine("Dropping field 'extra'...");
        await client.DropCollectionFieldAsync(new DropCollectionFieldReq
        {
            CollectionName = collectionName,
            FieldName = "extra"
        });
        await DescribeCollectionAsync(client, collectionName);
        #endregion

        #region Snippet:MilvusAddField_AddFunctionField
        // Add a function-backed (BM25) field: a text input plus a sparse output field with a bound index.
        // Adding a function field to a collection that already contains segments requires a Milvus server
        // configured with all three of these (mirroring the Java/C++ add_field examples and the server's
        // validateAddFunctionRequiresStorageV3 gate):
        //   - common.storage.useLoonFFI=true
        //   - dataCoord.compaction.bumpSchemaVersion.enabled=true
        //   - dataCoord.compaction.storageVersion.enabled=true
        await client.AddCollectionFieldAsync(new AddCollectionFieldReq
        {
            CollectionName = collectionName,
            Field = FieldSchema.CreateVarchar("text", maxLength: 100, nullable: true,
                enableAnalyzer: true, enableMatch: true)
        });
        Console.WriteLine("Adding function-backed field 'sparse'...");
        await client.AddFunctionFieldAsync(new AddFunctionFieldReq
        {
            CollectionName = collectionName,
            Field = FieldSchema.CreateSparseFloatVector("sparse"),
            Function = FunctionSchema.CreateBm25("bm25", "text", "sparse"),
            IndexParam = new IndexParam("sparse", "sparse_idx", IndexType.SparseInvertedIndex, MetricType.Bm25)
        });
        await DescribeCollectionAsync(client, collectionName);

        // Drop the function and its output field.
        Console.WriteLine("Dropping function-backed field 'sparse'...");
        await client.DropFunctionFieldAsync(new DropFunctionFieldReq
        {
            CollectionName = collectionName,
            FunctionName = "bm25"
        });
        await DescribeCollectionAsync(client, collectionName);
        #endregion

        Console.WriteLine("Dropping collection...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }

    private static async Task DescribeCollectionAsync(MilvusClientV2 client, string collectionName)
    {
        DescribeCollectionResp description = await client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = collectionName });
        Console.WriteLine($"  Fields: {string.Join(", ", description.Schema.Fields.Select(f => f.Name))}");
    }
}
