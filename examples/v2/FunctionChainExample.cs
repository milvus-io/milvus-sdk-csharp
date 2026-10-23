using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates a function chain applied to search results at the <see cref="FunctionChainStage.L2Rerank" />
/// stage: round the relevance score, sort descending, and keep the top-K hits.
/// Mirrors cpp examples/src/v2/function_chain.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to compose <see cref="FunctionChainOp.Map" />,
/// <see cref="FunctionChainOp.Sort" /> and <see cref="FunctionChainOp.Limit" /> operations into a
/// <see cref="FunctionChain" /> and attach it to a search request via <see cref="SearchReq.FunctionChains" />.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>, <c>InsertAsync</c>,
/// <c>FlushAsync</c>, <c>LoadCollectionAsync</c>, <c>SearchAsync</c> with a function chain,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> 10 hits under "Searching with function chain", each with a score rounded to
/// two decimals, sorted in descending score order, then "Done.".</para>
/// </remarks>
public static class FunctionChainExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri, "root:Milvus");
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "csharp_v2_function_chain";
        const string fieldId = "id";
        const string fieldVector = "vector";
        const int dimension = 128;
        const int rowCount = 1000;
        const int topk = 10;

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        Console.WriteLine("Creating collection...");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema(fieldId, DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector(fieldVector, dimension: dimension)
                }
            }
        });

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam(fieldVector, null, IndexType.Flat, MetricType.Cosine)]
        });

        // Insert rows whose vectors are drawn from a fixed pseudo-random sequence, so the cosine ranks are
        // stable across runs.
        Console.WriteLine($"Inserting {rowCount} rows...");
        var rows = new List<IDictionary<string, object?>>(rowCount);
        var random = new Random(42);
        for (long i = 0; i < rowCount; i++)
        {
            var vector = new float[dimension];
            for (int d = 0; d < dimension; d++)
            {
                vector[d] = (float)random.NextDouble();
            }

            rows.Add(new Dictionary<string, object?>
            {
                [fieldId] = i,
                [fieldVector] = vector
            });
        }

        await client.InsertAsync(new InsertReq { CollectionName = collectionName, RowsData = rows });

        // Flush so the freshly inserted data is searchable before the function chain search, then load.
        Console.WriteLine("Flushing...");
        await client.FlushAsync(new FlushReq { CollectionNames = new[] { collectionName } });
        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        // Use an all-1.0 vector to search.
        var query = new float[dimension];
        Array.Fill(query, 1.0f);

        // A function chain that runs at the L2_RERANK stage: round the score, sort descending, and limit.
        var expr = new FunctionChainExpr
        {
            Name = "round_decimal",
            Args = { FunctionChainArg.Column("$score") },
            Params = { ["decimal"] = FunctionParamValue.From(2) }
        };

        var chain = new FunctionChain
        {
            Stage = FunctionChainStage.L2Rerank,
            Name = "round_and_sort",
            Ops =
            {
                FunctionChainOp.Map("$score", expr),
                FunctionChainOp.Sort("$score", desc: true),
                FunctionChainOp.Limit(topk)
            }
        };

        Console.WriteLine($"Searching with function chain (topk={topk})...");
        SearchResp results = await client.SearchAsync(new SearchReq
        {
            CollectionName = collectionName,
            VectorFieldName = fieldVector,
            Vectors = new[] { new ReadOnlyMemory<float>(query) },
            MetricType = MetricType.Cosine,
            Limit = topk,
            FunctionChains = { chain },
            Parameters = new SearchParameters
            {
                OutputFields = { fieldId },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });

        SingleResult? single = results.SingleResults?.FirstOrDefault();
        if (single is not null)
        {
            for (int i = 0; i < single.Scores.Count; i++)
            {
                IReadOnlyDictionary<string, object?> row = single.GetRow(i);
                // round_decimal(2) rewrites $score; the reported score is the rounded value.
                Console.WriteLine($"  id={row[fieldId]} score={results.Scores[i]:F4}");
            }
        }

        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
