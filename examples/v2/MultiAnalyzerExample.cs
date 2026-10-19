using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates multi-analyzers on a single VARCHAR field: the analyzer is chosen at insert time
/// via a "language" column, and searched with a specific analyzer.
/// Mirrors cpp examples/src/v2/multi_analyzer.cpp and java MultiAnalyzerExample.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to configure several analyzers (english/chinese/.../default) for
/// one VARCHAR field and select one per row via <c>by_field</c>.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (with <c>MultiAnalyzerParams</c>),
/// <c>InsertAsync</c>, <c>QueryAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Query with analyzer returned N rows", then "Done.".</para>
/// </remarks>
public static class MultiAnalyzerExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "multi_analyzer_example";

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusMultiAnalyzer_Create
        var text = FieldSchema.CreateVarchar("text", maxLength: 65535, enableAnalyzer: true);
        text.MultiAnalyzerParams = new Dictionary<string, object>
        {
            ["analyzers"] = new Dictionary<string, object>
            {
                ["english"] = new Dictionary<string, object> { ["type"] = "english" },
                ["chinese"] = new Dictionary<string, object>
                {
                    ["tokenizer"] = "jieba",
                    ["filter"] = new[] { "lowercase", "removepunct" }
                },
                ["japanese"] = new Dictionary<string, object>
                {
                    ["tokenizer"] = new Dictionary<string, object>
                    {
                        ["type"] = "lindera",
                        ["dict_kind"] = "ipadic"
                    }
                },
                ["default"] = new Dictionary<string, object>
                {
                    ["tokenizer"] = "icu",
                    ["filter"] = new[] { "lowercase", "removepunct" }
                }
            },
            ["by_field"] = "language",
            ["alias"] = new Dictionary<string, object> { ["en"] = "english", ["cn"] = "chinese", ["jap"] = "japanese" }
        };

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    new FieldSchema("language", DataType.VarChar) { MaxLength = 100 },
                    text,
                    // A BM25 function over "text" produces a sparse vector, so the collection has a
                    // vector field the server requires.
                    new FieldSchema("sparse", DataType.SparseFloatVector)
                },
                Functions = { FunctionSchema.CreateBm25("function_bm25", "text", "sparse") }
            }
        });
        #endregion

        // Rows in English, Chinese and Japanese; the "language" column selects the analyzer per row.
        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam("sparse", null, IndexType.SparseInvertedIndex, MetricType.Bm25)]
        });

        Console.WriteLine("Inserting...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData =
            [
                new Dictionary<string, object?> { ["id"] = 1L, ["language"] = "en", ["text"] = "Hello Milvus!" },
                new Dictionary<string, object?> { ["id"] = 2L, ["language"] = "cn", ["text"] = "你好 向量数据库" },
                new Dictionary<string, object?> { ["id"] = 3L, ["language"] = "jap", ["text"] = "Milvusの新機能をご確認くださいこのページでは" }
            ]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        // BM25 full-text search on the analyzer-annotated text (the sparse vector is filled by the function).
        Console.WriteLine("Searching...");
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "sparse",
            Texts = new[] { "Milvus" },
            MetricType = MetricType.Bm25,
            Limit = 5,
            Parameters = new SearchParameters { OutputFields = { "id", "text" } }
        });
        Console.WriteLine($"Query with analyzer returned {results.Ids.LongIds?.Count ?? 0} row(s)");
        ExampleHelpers.PrintSearchHits(results);

        // The "analyzer_name" search parameter (sent via ExtraParameters, mirroring the C++/Java SDKs) selects
        // the japanese analyzer to tokenize the query text. "Milvus" appears in the japanese row, so the
        // japanese-tokenized search matches it.
        Console.WriteLine("Searching with japanese analyzer...");
        SearchResp japaneseResults = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = "sparse",
            Texts = new[] { "Milvus" },
            MetricType = MetricType.Bm25,
            Limit = 5,
            Parameters = new SearchParameters
            {
                OutputFields = { "id", "text", "language" },
                ExtraParameters = { ["analyzer_name"] = "japanese" }
            }
        });
        Console.WriteLine($"Query with japanese analyzer returned {japaneseResults.Ids.LongIds?.Count ?? 0} row(s)");
        ExampleHelpers.PrintSearchHits(japaneseResults);

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
