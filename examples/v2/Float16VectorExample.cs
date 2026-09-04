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
/// <c>Float16Vectors</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Search returned 3 hits; top score = 0.0000", then "Done.".</para>
/// </remarks>
public static class Float16VectorExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
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
                    new FieldSchema("vector", DataType.Float16Vector) { Dimension = 4 },
                    new FieldSchema("bf16", DataType.BFloat16Vector) { Dimension = 4 }
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

        // BF16 uses a different bit layout; encode the same floats as BF16 bit patterns.
        var bf16Rows = new[]
        {
            new ushort[] { Float16Utils.FloatToBf16(1f), Float16Utils.FloatToBf16(0f), 0, 0 },
            new ushort[] { Float16Utils.FloatToBf16(0f), Float16Utils.FloatToBf16(1f), 0, 0 },
            new ushort[] { Float16Utils.FloatToBf16(0f), 0, Float16Utils.FloatToBf16(1f), 0 }
        };
        #endregion

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new Milvus.Client.V2.Requests.Index.CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes =
            [
                new Milvus.Client.V2.Types.IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2),
                new Milvus.Client.V2.Types.IndexParam("bf16", "bf16_idx", IndexType.Flat, SimilarityMetricType.L2)
            ]
        });

        Console.WriteLine("Inserting 3 rows...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", new long[] { 1, 2, 3 }),
                new Float16VectorFieldData("vector", rows.Select(r => (ReadOnlyMemory<ushort>)r).ToArray()),
                new BFloat16VectorFieldData("bf16", bf16Rows.Select(r => (ReadOnlyMemory<ushort>)r).ToArray())
            ]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        Console.WriteLine("Searching...");
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Float16Vectors = new[]
            {
                (ReadOnlyMemory<ushort>)new[] { Float16Utils.FloatToFp16(1f), Float16Utils.FloatToFp16(0f), (ushort)0, (ushort)0 }
            },
            MetricType = SimilarityMetricType.L2,
            Limit = 3
        });

        Console.WriteLine($"Search returned {results.Ids.LongIds?.Count ?? 0} hits; top score = {results.Scores.FirstOrDefault():F4}");
        ExampleHelpers.PrintSearchHits(results);

        Console.WriteLine("Searching the BF16 field...");
        SearchResp bf16Results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "bf16",
            BFloat16Vectors = new[]
            {
                (ReadOnlyMemory<ushort>)new[] { Float16Utils.FloatToBf16(1f), Float16Utils.FloatToBf16(0f), (ushort)0, (ushort)0 }
            },
            MetricType = SimilarityMetricType.L2,
            Limit = 3
        });

        Console.WriteLine($"BF16 search returned {bf16Results.Ids.LongIds?.Count ?? 0} hits; top score = {bf16Results.Scores.FirstOrDefault():F4}");
        ExampleHelpers.PrintSearchHits(bf16Results);

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
