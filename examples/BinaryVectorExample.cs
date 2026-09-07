using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates binary vectors. Mirrors java BinaryVectorExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to define, insert and query a <c>BinaryVector</c> field, whose
/// rows are bit-packed <see cref="byte" /> values.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c> (with <c>BinaryVectorFieldData</c>), <c>QueryAsync</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Rows returned: 2", then "Done.".</para>
/// </remarks>
public static class BinaryVectorExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "binary_vector_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusBinaryVector_Schema
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("vector", DataType.BinaryVector) { Dimension = 16 }
                }
            }
        });
        #endregion

        // 16 bits => 2 bytes per vector.
        
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "vector",
            IndexType = IndexType.BinFlat,
            MetricType = SimilarityMetricType.Jaccard
        });

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2 }),
                new BinaryVectorFieldData("vector", new ReadOnlyMemory<byte>[]
                {
                    new byte[] { 0x0F, 0xF0 },
                    new byte[] { 0xFF, 0x00 }
                })
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        QueryResp results = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in [1, 2]",
            Parameters = new QueryParameters { OutputFields = { "vector" } }
        });
        Console.WriteLine($"Rows returned: {results.FieldsData.FirstOrDefault()?.RowCount ?? 0}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
