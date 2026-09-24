using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates upsert (insert-or-update). Mirrors java UpsertExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how <c>UpsertAsync</c> both updates an existing primary key and inserts
/// a new one in a single call.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>UpsertAsync</c>, <c>QueryAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Upsert count: 2", "Rows returned: 3", then "Done.".</para>
/// </remarks>
public static class UpsertExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "upsert_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("name", maxLength: 64),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });

        // Insert 2 rows, then upsert: update row 1 and add row 3.
        
        #region Snippet:MilvusUpsert_Upsert
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new Milvus.Client.V2.Types.IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2) ]
        });

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

        MutationResp upsert = await client.UpsertAsync(new UpsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 1, 3 }),
                FieldData.CreateVarChar("name", new[] { "a-updated", "c" }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 1f }),
                    new ReadOnlyMemory<float>(new[] { 0.5f, 0.5f })
                })
            ]
        });
        #endregion
        Console.WriteLine($"Upsert count: {upsert.UpsertCount}");

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        QueryResp results = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2, 3]",
            Parameters = new QueryParameters { OutputFields = { "id", "name" } }
        });
        Console.WriteLine($"Rows returned: {results.FieldsData.FirstOrDefault()?.RowCount ?? 0}");

        #region Snippet:MilvusUpsert_PartialUpdate
        // Partial update: only "name" is supplied (the vector field is omitted and left unchanged).
        // The id 1 row keeps its vector while its name is replaced.
        await client.UpsertAsync(new UpsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?> { ["id"] = 1L, ["name"] = "a-partially-updated" }
            ],
            PartialUpdate = true
        });
        #endregion

        QueryResp partial = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id == 1",
            Parameters = new QueryParameters { OutputFields = { "id", "name", "vector" } }
        });
        Console.WriteLine($"Partial update applied; row 1 present: {partial.FieldsData.FirstOrDefault()?.RowCount ?? 0}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
