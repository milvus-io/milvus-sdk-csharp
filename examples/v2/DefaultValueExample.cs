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
/// Demonstrates field default values: rows that omit a field with a default are filled in
/// by the server. Mirrors cpp examples/src/v2/default_value.cpp (java NullAndDefaultExample).
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show that a VARCHAR/float field declared with <c>DefaultValue</c> is
/// populated with that value when a row omits it.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (with <c>DefaultValue</c>), <c>InsertAsync</c>,
/// <c>QueryAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "default value is applied: rows=N", then "Done.".</para>
/// </remarks>
public static class DefaultValueExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "default_value_example";

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        var title = FieldSchema.CreateVarchar("title", maxLength: 100);
        title.DefaultValue = "N/A";
        var score = new FieldSchema("score", DataType.Float) { DefaultValue = 0.0f };
        var embedding = FieldSchema.CreateFloatVector("embedding", dimension: 4);

        Console.WriteLine("Creating collection with default values...");
        #region Snippet:MilvusDefaultValue_Create
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    title,
                    score,
                    embedding
                }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("embedding", null, IndexType.Flat, MetricType.L2)]
        });

        // "title" and "score" are omitted: the server fills in the defaults. A second row supplies
        // explicit values so a filter on the defaulted field can be demonstrated below.
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["embedding"] = new[] { 0.1f, 0.2f, 0.3f, 0.4f }
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["title"] = "Milvus",
                    ["score"] = 1.5f,
                    ["embedding"] = new[] { 0.5f, 0.6f, 0.7f, 0.8f }
                }
            ]
        });
        #endregion

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        Console.WriteLine("Querying...");
        QueryResp query = await client.QueryAsync(new QueryReq
        {
            CollectionName = collectionName,
            Expression = "id == 1",
            Parameters = new QueryParameters { OutputFields = { "id", "title", "score" } }
        });

        // The server filled in the default values; report the row count returned.
        Console.WriteLine($"default value is applied: returned {query.FieldsData.FirstOrDefault()?.RowCount ?? 0} row(s)");
        ExampleHelpers.PrintQueryRows(query);

        // Search with an expression filtering on the defaulted field: only the row whose "title" was
        // supplied explicitly (and thus differs from the default) should come back.
        Console.WriteLine("Searching filtered on the defaulted field...");
        SearchResp search = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "embedding",
            Vectors = new[] { new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f, 0.4f }) },
            MetricType = MetricType.L2,
            Limit = 10,
            Parameters = new SearchParameters
            {
                OutputFields = { "id", "title", "score" },
                Expression = "title != \"N/A\""
            }
        });
        Console.WriteLine($"search on title != \"N/A\" returned {search.SingleResults?.FirstOrDefault()?.Scores.Count ?? 0} hit(s)");
        ExampleHelpers.PrintSearchHits(search);

        Console.WriteLine("Deleting...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
