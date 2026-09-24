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
/// Demonstrates full-text search: a BM25 function turns a VARCHAR field into a sparse-vector index, and
/// searches run on natural-language text (embedded text). Also shows the <c>TEXT_MATCH</c> keyword filter.
/// Mirrors cpp examples/src/v2/full_text_match.cpp and java FullTextSearchExample / TextMatchExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show BM25-based lexical search end to end: define a <c>FunctionType.BM25</c>
/// function over a VARCHAR field, insert text rows, and search with plain text instead of a vector.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (with a BM25 <c>FunctionSchema</c>),
/// <c>CreateIndexAsync</c> (sparse-inverted-index + BM25 metric), <c>InsertAsync</c>, <c>SearchAsync</c>
/// with <c>Texts</c>, and a <c>TEXT_MATCH</c> filter expression.</para>
/// <para><b>Expected output:</b> hit rows for each text query, then "Done.".</para>
/// </remarks>
public static class FullTextSearchExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        await client.ConnectAsync();

        const string collectionName = "full_text_search_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusFullTextSearch_Create
        var schema = new CollectionSchema { Name = collectionName };
        schema.Fields.Add(new FieldSchema("id", DataType.Int64, isPrimaryKey: true));
        // The BM25 function fills this sparse vector field automatically from the text field.
        schema.Fields.Add(new FieldSchema("vector", DataType.SparseFloatVector));
        // TEXT_MATCH filtering requires enableMatch (in addition to enableAnalyzer for BM25).
        schema.Fields.Add(FieldSchema.CreateVarchar("text", maxLength: 65535, enableAnalyzer: true, enableMatch: true));
        // BM25 function: Milvus generates the sparse vectors for "text" into "vector" at insert time.
        schema.Functions.Add(FunctionSchema.CreateBm25("function_bm25", "text", "vector"));

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = schema
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [ new Milvus.Client.V2.Types.IndexParam("vector", null, IndexType.SparseInvertedIndex, SimilarityMetricType.Bm25) ]
        });
        #endregion

        string[] texts =
        {
            "Milvus is an open-source vector database",
            "AI applications help people live better",
            "Will the electric car replace gas-powered cars?",
            "LangChain is a composable framework to build with LLMs. Milvus is integrated into LangChain.",
            "RAG is the process of optimizing the output of a large language model",
            "Newton is one of the greatest scientists of human history",
            "Metric type L2 is Euclidean distance",
            "Embeddings represent real-world objects, like words, images, or videos, in a form that computers can process.",
            "The moon is 384,400 km distance away from earth",
            "Milvus supports L2 distance and IP similarity for float vectors."
        };

        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData = Enumerable.Range(0, texts.Length)
                .Select(i => (IDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["id"] = (long)i,
                    ["text"] = texts[i]
                })
                .ToList()
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        #region Snippet:MilvusFullTextSearch_Search
        await SearchByTextAsync(client, collectionName, "what is Milvus?");
        await SearchByTextAsync(client, collectionName, "electric car");

        // Keyword filter: only rows whose text contains "car".
        SearchResp match = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Texts = new[] { "electric car" },
            MetricType = SimilarityMetricType.Bm25,
            Limit = 5,
            Parameters = new SearchParameters
            {
                Expression = "TEXT_MATCH(text, \"car\")",
                OutputFields = { "id", "text" }
            }
        });
        Console.WriteLine($"TEXT_MATCH \"car\": {match.Ids.LongIds?.Count ?? 0} hits");
        #endregion

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }

    private static async Task SearchByTextAsync(
        MilvusClientV2 client, string collectionName, string text)
    {
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "vector",
            Texts = new[] { text },
            MetricType = SimilarityMetricType.Bm25,
            Limit = 5,
            Parameters = new SearchParameters { OutputFields = { "id", "text" } }
        });

        Console.WriteLine($"Search \"{text}\" returned {results.Ids.LongIds?.Count ?? 0} hits:");
        SingleResult? single = results.SingleResults?.FirstOrDefault();
        if (single is not null)
        {
            for (int i = 0; i < (results.Ids.LongIds?.Count ?? 0); i++)
            {
                IReadOnlyDictionary<string, object?> row = single.GetRow(i);
                Console.WriteLine($"  id={row["id"]} text=\"{row["text"]}\"");
            }
        }
    }
}
