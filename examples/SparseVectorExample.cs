using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates sparse vectors: insert MilvusSparseVector rows and search over them.
/// Mirrors cpp examples/src/v2/vector_sparse.cpp and java SparseVectorExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to define a <c>SparseFloatVector</c> field, insert
/// <see cref="MilvusSparseVector{T}" /> rows, and run a sparse-vector search.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c> (with
/// <c>SparseInvertedIndex</c>), <c>InsertAsync</c>, <c>SearchAsync</c> with
/// <c>SparseVectors</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Search returned 3 hits; top score = …", then "Done.".</para>
/// </remarks>
public static class SparseVectorExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "sparse_vector_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusSparseVector_Insert
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("sparse", DataType.SparseFloatVector)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            FieldName = "sparse",
            IndexType = IndexType.SparseInvertedIndex,
            MetricType = SimilarityMetricType.Ip
        });
        #endregion

        var vectors = new[]
        {
            new MilvusSparseVector<float>(new[] { 0, 2, 5 }, new[] { 0.1f, 0.2f, 0.5f }),
            new MilvusSparseVector<float>(new[] { 1, 3 }, new[] { 0.4f, 0.1f }),
            new MilvusSparseVector<float>(new[] { 0, 4 }, new[] { 0.3f, 0.6f })
        };

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            Data =
            [
                FieldData.Create("id", new long[] { 1, 2, 3 }),
                FieldData.CreateSparseFloatVector("sparse", vectors)
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        // Search with a sparse query vector.
        var query = new MilvusSparseVector<float>(new[] { 0, 2, 5 }, new[] { 0.1f, 0.2f, 0.5f });
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "sparse",
            SparseVectors = new[] { query },
            MetricType = SimilarityMetricType.Ip,
            Limit = 3
        });

        Console.WriteLine($"Search returned {results.Ids.LongIds?.Count ?? 0} hits; top score = {results.Scores.FirstOrDefault():F4}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
