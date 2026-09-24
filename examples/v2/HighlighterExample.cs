using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates lexical highlighting on BM25 full-text search results: configure pre/post tags and
/// fragment size, then read the highlighted fragments from the response.
/// Mirrors cpp examples/src/v2/highlighter.cpp and java HighlighterExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to highlight matched terms in a BM25 search via
/// <c>SearchParameters.Highlighter</c> and read the results from
/// <see cref="SearchResp.HighlightResults" />.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (BM25 function + analyzer),
/// <c>InsertAsync</c>, <c>SearchAsync</c> with <c>Texts</c> + <c>Highlighter</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> highlighted fragment text for a "Milvus" query, then "Done.".</para>
/// </remarks>
public static class HighlighterExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "highlighter_example";

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusHighlighter_Create
        var schema = new CollectionSchema { Name = collectionName };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        schema.Fields.Add(FieldSchema.CreateVarchar("title", maxLength: 256));
        schema.Fields.Add(FieldSchema.CreateVarchar("text", maxLength: 65535, enableAnalyzer: true));
        schema.Fields.Add(new FieldSchema("sparse", DataType.SparseFloatVector));
        schema.Functions.Add(FunctionSchema.CreateBm25("function_bm25", "text", "sparse"));

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = schema
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("sparse", null, IndexType.SparseInvertedIndex, SimilarityMetricType.Bm25)]
        });

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?>
                {
                    ["id"] = 1L,
                    ["title"] = "Milvus for scale",
                    ["text"] = "Milvus is an open-source vector database built for scale."
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2L,
                    ["title"] = "Full-text search",
                    ["text"] = "Milvus supports full text search with analyzers and BM25."
                }
            ]
        });
        #endregion

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        #region Snippet:MilvusHighlighter_Search
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "sparse",
            Texts = new[] { "Milvus" },
            MetricType = SimilarityMetricType.Bm25,
            Limit = 5,
            Parameters = new SearchParameters
            {
                OutputFields = { "title", "text" },
                Highlighter =
                {
                    ["pre_tag"] = "<em>",
                    ["post_tag"] = "</em>",
                    ["fragment_size"] = "40",
                    ["fragment_num"] = "10"
                },
                HighlightType = HighlightType.Lexical
            }
        });
        #endregion

        MilvusHighlightResult? highlight = results.HighlightResults?.FirstOrDefault(h => h.FieldName == "text");
        Console.WriteLine(highlight is null
            ? "No highlight results returned."
            : $"Highlighted \"text\": {string.Join(" ", highlight.Datas.SelectMany(d => d.Fragments))}");

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
