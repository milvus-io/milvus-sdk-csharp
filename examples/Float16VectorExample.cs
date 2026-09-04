using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates float16 vectors: insert half-precision rows and search with a float16 query.
/// Mirrors cpp examples/src/v2/vector_fp16.cpp and java Float16VectorExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to encode <see cref="float" /> values as FP16 bit patterns via
/// <see cref="Float16Utils" /> and use them in insert and search.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c> (with <c>Float16VectorFieldData</c>), <c>SearchAsync</c> with
/// <c>HalfVectors</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Search returned 3 hits; top score = 0.0000", then "Done.".</para>
/// </remarks>
public static class Float16VectorExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "float16_vector_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusFloat16Vector_Insert
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("vector", DataType.Float16Vector) { Dimension = 4 }
                }
            }
        });

        // Encode floats as FP16 bit patterns (ushort) via Float16Utils.
        var rows = new[]
        {
            new ushort[] { Float16Utils.FloatToFp16(1f), Float16Utils.FloatToFp16(0f), 0, 0 },
            new ushort[] { Float16Utils.FloatToFp16(0f), Float16Utils.FloatToFp16(1f), 0, 0 },
            new ushort[] { Float16Utils.FloatToFp16(0f), 0, Float16Utils.FloatToFp16(1f), 0 }
        };
        #endregion

        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "vector",
            IndexType = IndexType.Flat,
            MetricType = SimilarityMetricType.L2
        });

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2, 3 }),
                new Float16VectorFieldData("vector", rows.Select(r => (ReadOnlyMemory<ushort>)r).ToArray())
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            HalfVectors = new[]
            {
                (ReadOnlyMemory<ushort>)new[] { Float16Utils.FloatToFp16(1f), Float16Utils.FloatToFp16(0f), (ushort)0, (ushort)0 }
            },
            MetricType = SimilarityMetricType.L2,
            Limit = 3
        });

        Console.WriteLine($"Search returned {results.Ids.LongIds?.Count ?? 0} hits; top score = {results.Scores.FirstOrDefault():F4}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
