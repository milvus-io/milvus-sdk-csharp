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
/// Demonstrates server-side iterators: page over all rows of a query, or all nearest
/// neighbours of a search, in <c>BatchSize</c>-sized batches via <c>await foreach</c>.
/// Mirrors cpp examples/src/v2/iterator_query.cpp and iterator_search.cpp, and java IteratorExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to consume a large result set without loading it at once:
/// <c>QueryIteratorAsync</c> (primary-key-ordered) and <c>SearchIteratorAsync</c>
/// (top-K ordered). Both are <see cref="IAsyncEnumerable{T}" />.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>QueryIteratorAsync</c>, <c>SearchIteratorAsync</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> a row count per query-iterator page, a hit count per
/// search-iterator page, then "Done.".</para>
/// </remarks>
public static class IteratorExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "iterator_example";
        const int dimension = 4;
        const int rowCount = 100;

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: dimension)
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("vector", null, IndexType.Flat, SimilarityMetricType.L2)]
        });

        var vectors = Enumerable.Range(0, rowCount)
            .Select(i => new ReadOnlyMemory<float>(new[] { i % 10 / 10f, i % 7 / 10f, i % 5 / 10f, i % 3 / 10f }))
            .ToArray();

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", Enumerable.Range(0, rowCount).Select(i => (long)i).ToArray()),
                FieldData.CreateFloatVector("vector", vectors)
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        #region Snippet:MilvusIterator_Query
        Console.WriteLine("Query iterator (batch size 10):");
        int queryRows = 0;
        await foreach (IReadOnlyList<FieldData> batch in client.QueryIteratorAsync(
            new QueryIteratorReq
            {
                CollectionName = collectionName,
                BatchSize = 10,
                Parameters = new QueryParameters { Limit = 50 }
            }))
        {
            Console.WriteLine($"  page of {batch.FirstOrDefault()?.RowCount ?? 0} rows");
            queryRows += batch.FirstOrDefault()?.RowCount ?? 0;
        }

        Console.WriteLine($"  total rows iterated: {queryRows}");
        #endregion

        #region Snippet:MilvusIterator_Search
        Console.WriteLine("Search iterator (batch size 10):");
        int searchHits = 0;
        await foreach (SingleResult batch in client.SearchIteratorAsync(
            new SearchIteratorReq
            {
                CollectionName = collectionName,
                VectorFieldName = "vector",
                Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.5f, 0.5f, 0.5f, 0.5f }) },
                MetricType = SimilarityMetricType.L2,
                BatchSize = 10,
                Limit = 50
            }))
        {
            Console.WriteLine($"  page of {batch.Scores.Count} hits");
            searchHits += batch.Scores.Count;
        }

        Console.WriteLine($"  total hits iterated: {searchHits}");
        #endregion

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
