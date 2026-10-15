using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates an array field: schema with an int array, insert array rows, query them back.
/// Mirrors cpp examples/src/v2/array.cpp and java ArrayFieldExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to model a scalar array column and round-trip it with
/// <see cref="ArrayFieldData{TElement}" />.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>InsertAsync</c>,
/// <c>CreateIndexAsync</c>, <c>QueryAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Rows returned: 2", then "Done.".</para>
/// </remarks>
public static class ArrayFieldExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "array_field_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusArrayField_Schema
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("scores", DataType.Array) { MaxCapacity = 5, ElementDataType = DataType.Int64 },
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });
        #endregion

        Console.WriteLine("Inserting...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                new ArrayFieldData<long>("scores", new IReadOnlyList<long>[]
                {
                    new long[] { 80, 90 },
                    new long[] { 70, 75, 85 }
                }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f })
                })
            ]
        });

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new Milvus.Client.V2.Types.IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2) ]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        Console.WriteLine("Querying...");
        QueryResp results = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2]",
            Parameters = new QueryParameters { OutputFields = { "scores" } }
        });
        Console.WriteLine($"Rows returned: {results.FieldsData.FirstOrDefault()?.RowCount ?? 0}");
        ExampleHelpers.PrintQueryRows(results);

        // Array-filter expressions (array_contains / array_length) query inside the array column.
        Console.WriteLine("Querying with array filters...");
        QueryResp contains = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "array_contains(scores, 80)",
            Parameters = new QueryParameters { OutputFields = { "id", "scores" } }
        });
        Console.WriteLine($"Rows whose scores contain 80: {contains.FieldsData.FirstOrDefault()?.RowCount ?? 0}");
        ExampleHelpers.PrintQueryRows(contains);

        QueryResp length = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "array_length(scores) == 3",
            Parameters = new QueryParameters { OutputFields = { "id", "scores" } }
        });
        Console.WriteLine($"Rows with 3 scores: {length.FieldsData.FirstOrDefault()?.RowCount ?? 0}");
        ExampleHelpers.PrintQueryRows(length);

        Console.WriteLine("Dropping collection...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
