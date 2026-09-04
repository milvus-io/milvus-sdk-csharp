using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates int8 vectors. Mirrors cpp examples/src/v2/vector_int8.cpp and java Int8VectorExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to define, insert and query an <c>Int8Vector</c> field, whose rows
/// are <see cref="sbyte" /> values.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c> (with <c>Int8VectorFieldData</c>), <c>QueryAsync</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Rows returned: 2", then "Done.".</para>
/// </remarks>
public static class Int8VectorExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "int8_vector_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusInt8Vector_Insert
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("vector", DataType.Int8Vector) { Dimension = 4 }
                }
            }
        });

        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "vector",
            IndexType = IndexType.Hnsw,
            MetricType = SimilarityMetricType.L2
        });
        #endregion

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                new Int8VectorFieldData("vector", new ReadOnlyMemory<sbyte>[]
                {
                    new sbyte[] { 1, 2, 3, 4 },
                    new sbyte[] { -1, -2, -3, -4 }
                })
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        QueryResp results = await client.QueryAsync(new Milvus.Client.V2.Requests.Dql.QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2]",
            Parameters = new Milvus.Client.V2.Types.QueryParameters { OutputFields = { "vector" } }
        });
        Console.WriteLine($"Rows returned: {results.FieldsData.FirstOrDefault()?.RowCount ?? 0}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
