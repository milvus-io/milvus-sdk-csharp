using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
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
/// <c>InsertAsync</c>, <c>CreateIndexAsync</c>, <c>QueryAsync</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Rows returned: 2", then "Done.".</para>
/// </remarks>
public static class AddFieldExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
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

        await client.AddCollectionFieldAsync(new AddCollectionFieldReq
        {
            CollectionName = collectionName,
            Field = FieldSchema.CreateVarchar("extra", maxLength: 64, nullable: true)
        });
        #endregion

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                FieldData.CreateVarChar("name", new[] { "a", "b" }),
                FieldData.CreateVarChar("extra", new[] { "x1", "x2" }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f })
                })
            ]
        });

        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "vector",
            IndexType = IndexType.Flat,
            MetricType = SimilarityMetricType.L2
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        QueryResp results = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2]",
            Parameters = new QueryParameters { OutputFields = { "id", "extra" } }
        });
        Console.WriteLine($"Rows returned: {results.FieldsData.FirstOrDefault()?.RowCount ?? 0}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
