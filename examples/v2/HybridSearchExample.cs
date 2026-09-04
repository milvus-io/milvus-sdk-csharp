using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates hybrid search: multiple ANN sub-requests (dense + sparse) combined with a
/// weighted reranker. Mirrors cpp examples/src/v2/hybrid_search.cpp and java HybridSearchExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to search a collection with both a dense and a sparse vector field
/// in one call, fusing the results with <see cref="WeightedReranker" /> (or
/// <see cref="RrfReranker" />).</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c> (dense DISKANN +
/// sparse inverted), <c>InsertAsync</c>, <c>HybridSearchAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Hybrid search returned N hits", then "Done.".</para>
/// </remarks>
public static class HybridSearchExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "hybrid_search_example";
        const int dimension = 128;
        const int rowCount = 100;

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusHybridSearch_Create
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("dense", dimension: dimension),
                    new FieldSchema("sparse", DataType.SparseFloatVector)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes =
            [
                new IndexParam("dense", "dense_idx", IndexType.AutoIndex, SimilarityMetricType.Cosine),
                new IndexParam("sparse", "sparse_idx", IndexType.SparseInvertedIndex, SimilarityMetricType.Ip)
            ]
        });
        #endregion

        // Insert rows, each with a dense vector and a sparse vector.
        var rows = new List<IDictionary<string, object?>>();
        for (long i = 0; i < rowCount; i++)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["id"] = i,
                ["dense"] = DenseVector(i, dimension),
                ["sparse"] = new MilvusSparseVector<float>(new[] { (int)(i % 10), (int)(i % 7) + 10 }, new[] { 0.1f, 0.2f })
            });
        }

        await client.InsertAsync(new InsertReq { CollectionName = collectionName, RowsData = rows });
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        #region Snippet:MilvusHybridSearch_Search
        SearchResp results = await client.HybridSearchAsync(new HybridSearchReq
        {
            CollectionName = collectionName,
            SearchRequests =
            [
                new AnnSearchReq
                {
                    VectorFieldName = "dense",
                    Vectors = new[] { DenseVector(0, dimension) },
                    MetricType = SimilarityMetricType.Cosine,
                    Limit = 5
                },
                new AnnSearchReq
                {
                    VectorFieldName = "sparse",
                    SparseVectors = new[]
                    {
                        new MilvusSparseVector<float>(new[] { 0, 10 }, new[] { 0.1f, 0.2f })
                    },
                    MetricType = SimilarityMetricType.Ip,
                    Limit = 5
                }
            ],
            Reranker = new WeightedReranker(0.5f, 0.5f),
            Limit = 5
        });
        #endregion

        Console.WriteLine($"Hybrid search returned {results.Ids.LongIds?.Count ?? 0} hits; top score = {results.Scores.FirstOrDefault():F4}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }

    private static ReadOnlyMemory<float> DenseVector(long seed, int dimension)
    {
        var values = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            values[i] = ((seed * 31 + i) % 100) / 100f;
        }

        return values;
    }
}
