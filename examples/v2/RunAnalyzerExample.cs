using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Utility;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates the text analyzer. Mirrors cpp examples/src/v2/run_analyzer.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to tokenize text with a given analyzer configuration without
/// ingesting it, using <c>RunAnalyzerAsync</c>.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (varchar field with <c>enableAnalyzer</c>),
/// <c>RunAnalyzerAsync</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> "Tokens: hello, milvus, full, text, search" (standard tokenizer),
/// then "Done.".</para>
/// </remarks>
public static class RunAnalyzerExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "run_analyzer_example";
        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        #region Snippet:MilvusRunAnalyzer_Analyze
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar("text", maxLength: 256, enableAnalyzer: true),
                    FieldSchema.CreateSparseFloatVector("sparse")
                }
            }
        });

        RunAnalyzerResp result = await client.RunAnalyzerAsync(new RunAnalyzerReq
        {
            AnalyzerParams = new Dictionary<string, object> { ["tokenizer"] = "standard" },
            Texts = new[] { "Hello Milvus! Full-text search." }
        });
        #endregion

        foreach (AnalyzerResult r in result.Results)
        {
            Console.WriteLine($"Tokens: {string.Join(", ", r.Tokens.Select(t => t.Token))}");
        }

        // A jieba tokenizer for Chinese text.
        RunAnalyzerResp jiebaResult = await client.RunAnalyzerAsync(new RunAnalyzerReq
        {
            AnalyzerParams = new Dictionary<string, object> { ["tokenizer"] = "jieba" },
            Texts = new[] { "Milvus 是一个开源的向量数据库" }
        });
        foreach (AnalyzerResult r in jiebaResult.Results)
        {
            Console.WriteLine($"Jieba tokens: {string.Join(", ", r.Tokens.Select(t => t.Token))}");
        }

        // A whitespace tokenizer.
        RunAnalyzerResp whitespaceResult = await client.RunAnalyzerAsync(new RunAnalyzerReq
        {
            AnalyzerParams = new Dictionary<string, object> { ["tokenizer"] = "whitespace" },
            Texts = new[] { "Hello Milvus! Full-text search." }
        });
        foreach (AnalyzerResult r in whitespaceResult.Results)
        {
            Console.WriteLine($"Whitespace tokens: {string.Join(", ", r.Tokens.Select(t => t.Token))}");
        }

        Console.WriteLine("Dropping collection...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
