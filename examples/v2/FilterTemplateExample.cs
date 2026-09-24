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
/// Demonstrates filter templates: an expression with <c>{alias}</c> placeholders whose values are
/// supplied separately, avoiding hand-built IN lists. Mirrors cpp examples/src/v2/filter_template.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to parameterize a query or search filter with
/// <c>FilterTemplates</c> so large id/text lists do not need to be string-built.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>QueryAsync</c> and <c>SearchAsync</c> with
/// <c>FilterTemplates</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> the number of rows matched by the templated id filter, then "Done.".</para>
/// </remarks>
public static class FilterTemplateExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "filter_template_example";
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
            .Select(i => new ReadOnlyMemory<float>(new[] { i % 10 / 10f, i % 5 / 10f, i % 4 / 10f, i % 3 / 10f }))
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

        #region Snippet:MilvusFilterTemplate_Query
        // The {my_ids} alias is filled by the template value (a long[]), not by string interpolation.
        var filterIds = Enumerable.Range(10, 20).Select(i => (long)i).ToArray();

        QueryResp query = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id in {my_ids}",
            Parameters = new QueryParameters
            {
                OutputFields = { "id" },
                FilterTemplates = { ["my_ids"] = filterIds }
            }
        });

        Console.WriteLine($"Query with filter template matched {query.FieldsData.FirstOrDefault()?.RowCount ?? 0} rows");
        #endregion

        #region Snippet:MilvusFilterTemplate_Search
        SearchResp search = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.5f, 0.5f, 0.5f, 0.5f }) },
            MetricType = SimilarityMetricType.L2,
            Limit = 10,
            Parameters = new SearchParameters
            {
                Expression = "id in {my_ids}",
                FilterTemplates = { ["my_ids"] = filterIds }
            }
        });

        Console.WriteLine($"Search with filter template returned {search.Ids.LongIds?.Count ?? 0} hits");
        #endregion

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
