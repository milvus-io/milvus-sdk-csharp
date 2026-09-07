using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates group-by search. Mirrors cpp examples/src/v2/group_by.cpp and java GroupByExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to return a bounded number of hits per distinct value of a field
/// (here one hit per <c>category</c>).</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>SearchAsync</c> with <c>GroupByField</c>/<c>GroupSize</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Grouped search returned 2 hits", then "Done.".</para>
/// </remarks>
public static class GroupByExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "group_by_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusGroupBy_Search
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("category", DataType.Int64),
                    FieldSchema.CreateFloatVector("vector", dimension: 2)
                }
            }
        });

        
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
                FieldData.Create("id", new long[] { 1, 2, 3, 4 }),
                FieldData.Create("category", new long[] { 1, 1, 2, 2 }),
                FieldData.CreateFloatVector("vector", new[]
                {
                    new ReadOnlyMemory<float>(new[] { 1f, 0f }),
                    new ReadOnlyMemory<float>(new[] { 0.9f, 0.1f }),
                    new ReadOnlyMemory<float>(new[] { 0f, 1f }),
                    new ReadOnlyMemory<float>(new[] { 0.1f, 0.9f })
                })
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 1f, 0f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 4,
            Parameters = new SearchParameters
            {
                GroupByField = "category",
                GroupSize = 1,
                OutputFields = { "category" }
            }
        });
        #endregion
        Console.WriteLine($"Grouped search returned {results.Ids.LongIds?.Count ?? 0} hits");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
