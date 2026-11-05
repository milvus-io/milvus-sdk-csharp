using System.Globalization;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates the server-side search iterator, mirroring cpp examples/src/v2/iterator_search.cpp: a single
/// parameterized iteration function exercised over several (batch, limit, filter) combinations, a client-side
/// page filter, and a fresh collection built per metric (L2 and COSINE).
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to consume a large search result set page by page without loading it at
/// once. The search iterator is top-K ordered and yields one <see cref="SingleResult" /> per page, using the
/// server-side <c>search_iter_v2</c> token protocol.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>, <c>InsertAsync</c>,
/// <c>SearchIteratorAsync</c>, <c>SearchIteratorReq.ExternalFilterFunc</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> a hit count and the first/last hit per page for each iteration combination,
/// an external-filter demo that keeps only even ages, then "Done.".</para>
/// </remarks>
public static class IteratorSearchExample
{
    private const string CollectionName = "iterator_search_example";
    private const string IdField = "user_id";
    private const string NameField = "user_name";
    private const string AgeField = "user_age";
    private const string FaceField = "user_face";
    private const int Dimension = 128;

    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        // Run the whole demo once per metric, like the cpp example.
        await IterateMetric(client, MetricType.L2);
        await IterateMetric(client, MetricType.Cosine);

        Console.WriteLine("Deleting...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = CollectionName });
        Console.WriteLine("Done.");
    }

    private static async Task IterateMetric(MilvusClientV2 client, MetricType metric)
    {
        await BuildCollection(client, metric);

        // A single parameterized iteration function, exercised with the same (batch, limit, filter)
        // combinations as the cpp example. C++ uses limit -1 for "unlimited"; C# expresses that as
        // int.MaxValue.
        await IterateSearch(client, metric, 3000, 100000, "");
        await IterateSearch(client, metric, 25, 80, "");
        await IterateSearch(client, metric, 5000, int.MaxValue, "");
        await IterateSearch(client, metric, 100, int.MaxValue, $"{AgeField} == 8");
        await IterateSearch(client, metric, 1000, 2500, $"{AgeField} > 30");
        await IterateSearch(client, metric, 1000, 100000, $"{AgeField} in [30, 40, 50]");

        await IterateSearchWithExternalFilter(client, metric, 3000, 100000, "");
    }

    private static async Task BuildCollection(MilvusClientV2 client, MetricType metric)
    {
        await ExampleHelpers.ResetCollectionAsync(client, CollectionName);

        Console.WriteLine("Creating collection...");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = CollectionName,
            Schema = new CollectionSchema
            {
                EnableDynamicFields = true,
                Fields =
                {
                    new FieldSchema(IdField, DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar(NameField, maxLength: 100),
                    new FieldSchema(AgeField, DataType.Int8),
                    FieldSchema.CreateFloatVector(FaceField, dimension: Dimension)
                }
            }
        });

        Console.WriteLine("Creating index...");
        // Note that we use FLAT here, because other indexes cannot ensure that all entities can be fetched by
        // the iterator.
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = CollectionName,
            Indexes = [new IndexParam(FaceField, null, IndexType.Flat, metric)]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = CollectionName });

        // Insert rows with unordered primary keys, mirroring the cpp example: 5 seed ranges x 10000 rows each.
        foreach (long seed in new long[] { 50000, 10000, 30000, 90000, 0 })
        {
            var rows = new List<IDictionary<string, object?>>(10000);
            for (int k = 0; k < 10000; k++)
            {
                long id = seed + k;
                rows.Add(new Dictionary<string, object?>
                {
                    [IdField] = id,
                    [NameField] = $"my name is {id}",
                    [AgeField] = (sbyte)(k % 100),
                    [FaceField] = GenerateVector(id),
                    ["a"] = id,                            // dynamic field "a"
                    ["b"] = $"b is {id}"                    // dynamic field "b"
                });
            }

            MutationResp resp = await client.InsertAsync(new InsertReq { CollectionName = CollectionName, RowsData = rows });
            Console.WriteLine($"{resp.InsertCount} rows inserted.");
        }

        Console.WriteLine($"count(*) = {await GetRowCountAsync(client)}");
    }

    private static async Task IterateSearch(
        MilvusClientV2 client, MetricType metric, int batch, int limit, string filter)
    {
        #region Snippet:MilvusIterator_Search
        Console.WriteLine("=====================================================");
        Console.WriteLine($"Iterate batch: {batch} limit: {limit} filter: {filter}");

        var request = new SearchIteratorReq
        {
            CollectionName = CollectionName,
            VectorFieldName = FaceField,
            Vectors = new[] { new ReadOnlyMemory<float>(Enumerable.Repeat(1f, Dimension).ToArray()) },
            MetricType = metric,
            BatchSize = batch,
            Limit = limit,
            Parameters = new SearchParameters
            {
                // Strong pins a fresh snapshot so the verification below sees every row just inserted
                // (the collection default Bounded level may read a slightly stale view).
                ConsistencyLevel = ConsistencyLevel.Strong,
                Expression = filter,
                OutputFields = { NameField, AgeField, "b" }
            }
        };

        var ids = new HashSet<long>();
        int pages = 0;
        long totalCount = 0;
        await foreach (SingleResult page in client.SearchIteratorAsync(request))
        {
            int pageCount = page.Scores.Count;
            if (pageCount == 0)
            {
                Console.WriteLine("search iteration finished");
                break;
            }

            pages++;
            totalCount += pageCount;
            Console.WriteLine($"No.{pages} page {pageCount} rows fetched");
            Console.WriteLine($"\tthe first row: {ExampleHelpers.FormatRow(page.OutputFields, 0)}");
            Console.WriteLine($"\tthe last row: {ExampleHelpers.FormatRow(page.OutputFields, pageCount - 1)}");

            if (page.Ids.LongIds is { } longIds)
            {
                for (int i = 0; i < pageCount; i++)
                {
                    ids.Add(longIds[i]);
                }
            }
        }
        #endregion

        // Verify the number of returned ids is expected; only check when the filter is empty, because
        // filtering is unpredictable.
        if (filter.Length == 0)
        {
            long rowCount = await GetRowCountAsync(client);
            long expected = limit == int.MaxValue ? rowCount : Math.Min(limit, rowCount);
            if (ids.Count != expected)
            {
                Console.WriteLine($"Returned row count is unexpected: {ids.Count} returned vs limit {expected}");
                Console.WriteLine("Possible reason: some vectors' distances are equal, search engine could not distinct");
            }
        }

        Console.WriteLine($"Total fetched rows: {totalCount}");
        Console.WriteLine("=====================================================");
    }

    private static async Task IterateSearchWithExternalFilter(
        MilvusClientV2 client, MetricType metric, int batch, int limit, string filter)
    {
        #region Snippet:MilvusIterator_Search_ExternalFilter
        Console.WriteLine("=====================================================");
        Console.WriteLine($"Iterate with external filter, batch: {batch} limit: {limit} filter: {filter}");

        var request = new SearchIteratorReq
        {
            CollectionName = CollectionName,
            VectorFieldName = FaceField,
            Vectors = new[] { new ReadOnlyMemory<float>(Enumerable.Repeat(1f, Dimension).ToArray()) },
            MetricType = metric,
            BatchSize = batch,
            Limit = limit,
            Parameters = new SearchParameters
            {
                ConsistencyLevel = ConsistencyLevel.Strong,
                Expression = filter,
                OutputFields = { NameField, AgeField, "b" }
            },
            // The callback receives each fetched page and returns the 0-based row indices to keep. Here we
            // drop every hit whose "age" output field is odd.
            ExternalFilterFunc = page =>
            {
                FieldData? ageFieldData = page.OutputFields.FirstOrDefault(f => f.FieldName == AgeField);
                if (ageFieldData is null)
                {
                    throw new ArgumentException("user_age output field missing in search result");
                }

                var ages = (FieldData<sbyte>)ageFieldData;
                return Enumerable.Range(0, ages.Data.Count)
                    .Where(i => ages.Data[i] % 2 == 0)
                    .ToList();
            }
        };

        int pages = 0;
        long totalCount = 0;
        await foreach (SingleResult page in client.SearchIteratorAsync(request))
        {
            int pageCount = page.Scores.Count;
            if (pageCount == 0)
            {
                Console.WriteLine("search iteration with external filter finished");
                break;
            }

            pages++;
            totalCount += pageCount;
            Console.WriteLine($"No.{pages} page {pageCount} rows fetched");

            for (int i = 0; i < pageCount; i++)
            {
                if (((sbyte)page.GetRow(i)[AgeField]!) % 2 != 0)
                {
                    Console.WriteLine("ERROR: a row with odd user_age slipped through the external filter");
                }
            }
        }
        #endregion

        Console.WriteLine($"Total fetched rows (external filter): {totalCount}");
        Console.WriteLine("=====================================================");
    }

    private static async Task<long> GetRowCountAsync(MilvusClientV2 client)
    {
        QueryResp response = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Parameters = new QueryParameters
            {
                // Strong consistency so the count sees every row just inserted (the collection default
                // Bounded level may read a slightly stale view); same pattern as SimpleExample.
                OutputFields = { "count(*)" },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });

        return Convert.ToInt64(response.GetRow(0)["count(*)"], CultureInfo.InvariantCulture);
    }

    private static float[] GenerateVector(long id)
    {
        // Random floats in [0, 1), like the cpp example's util::GenerateFloatVector, so the search iterator
        // does not hit ties that the server cannot distinct.
        var random = new Random(unchecked((int)id));
        var vector = new float[Dimension];
        for (int d = 0; d < Dimension; d++)
        {
            vector[d] = (float)random.NextDouble();
        }

        return vector;
    }
}
