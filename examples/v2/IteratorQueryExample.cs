using System.Globalization;
using System.Text.Json;

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
/// Demonstrates the server-side query iterator, mirroring cpp examples/src/v2/iterator_query.cpp: a single
/// parameterized iteration function exercised over several (batch, offset, limit, filter) combinations, a
/// resumable cursor capture after the first page, and element-filter iteration over a struct-array field.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to consume a large query result set page by page without loading it at
/// once. The query iterator is primary-key-ordered and yields <c>IReadOnlyList&lt;FieldData&gt;</c> batches;
/// an <c>element_filter</c> expression over a struct-array field additionally expands each matched entity into
/// one row per matched element, with an <c>offset</c> column holding the element positions.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c> (with dynamic fields and a struct-array field),
/// <c>CreateIndexAsync</c>, <c>InsertAsync</c>, <c>QueryIteratorAsync</c>,
/// <c>QueryIterator.GetCursor()</c>, <c>QueryIteratorReq.Cursor</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> a row count and the first/last row per page for each iteration combination, a
/// resumed page after cursor capture, matched-element rows for the element-filter demo, then "Done.".</para>
/// </remarks>
public static class IteratorQueryExample
{
    private const string CollectionName = "iterator_query_example";
    private const string IdField = "user_id";
    private const string NameField = "user_name";
    private const string AgeField = "user_age";
    private const string TagsField = "user_tags";
    private const string FaceField = "user_face";
    private const int Dimension = 128;

    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        await ExampleHelpers.ResetCollectionAsync(client, CollectionName);

        Console.WriteLine("Creating collection...");
        // The struct-array field user_tags carries a VarChar sub-field "tag" that the element_filter demo
        // matches inside each struct element ($[tag]); dynamic fields are enabled like the cpp example.
        var tagsSchema = new StructFieldSchema(TagsField, "user tags") { MaxCapacity = 10 };
        tagsSchema.Fields.Add(FieldSchema.CreateVarchar("tag", maxLength: 100));

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
                },
                StructFields = { tagsSchema }
            }
        });

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = CollectionName,
            Indexes = [new IndexParam(FaceField, null, IndexType.AutoIndex, MetricType.L2)]
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = CollectionName });

        // Insert rows with unordered primary keys, mirroring the cpp example: 5 seed ranges x 10000 rows each.
        // The pks become 50000~59999, 10000~19999, 30000~39999, 90000~99999, 0~9999.
        foreach (long seed in new long[] { 50000, 10000, 30000, 90000, 0 })
        {
            var rows = new List<IDictionary<string, object?>>(10000);
            for (int k = 0; k < 10000; k++)
            {
                long id = seed + k;

                // Some rows carry a "news" tag element (some even twice) so the element_filter demo below has
                // matching elements to iterate over.
                var tags = new List<object> { new Dictionary<string, object?> { ["tag"] = "sports" } };
                if (k % 3 == 0)
                {
                    tags.Add(new Dictionary<string, object?> { ["tag"] = "news" });
                }

                if (k % 7 == 0)
                {
                    tags.Add(new Dictionary<string, object?> { ["tag"] = "news" });
                }

                rows.Add(new Dictionary<string, object?>
                {
                    [IdField] = id,
                    [NameField] = $"my name is {id}",
                    [AgeField] = (sbyte)(k % 100),
                    [TagsField] = tags,
                    [FaceField] = GenerateVector(id),
                    ["a"] = id,                            // dynamic field "a"
                    ["b"] = $"b is {id}"                    // dynamic field "b"
                });
            }

            MutationResp resp = await client.InsertAsync(new InsertReq { CollectionName = CollectionName, RowsData = rows });
            Console.WriteLine($"{resp.InsertCount} rows inserted.");
        }

        long rowCount = await GetRowCountAsync(client);
        Console.WriteLine($"count(*) = {rowCount}");

        // A single parameterized iteration function, exercised with the same (batch, offset, limit, filter)
        // combinations as the cpp example. C++ uses limit -1 for "unlimited"; C# expresses that as null.
        await IterateQuery(client, 3000, 25000, 100000, "", rowCount);
        await IterateQuery(client, 25, 100, 80, "", rowCount);
        await IterateQuery(client, 5000, 0, null, "", rowCount);
        await IterateQuery(client, 100, 0, null, $"{AgeField} == 8", rowCount);
        await IterateQuery(client, 1000, 15000, 2500, $"{AgeField} > 30", rowCount);
        await IterateQuery(client, 1000, 0, 100000, $"{AgeField} in [30, 40, 50]", rowCount);

        // Capture a cursor after the first page and resume a brand-new iterator from it.
        await IterateWithCursor(client);

        // Iterate the matched elements of an element_filter expression over the struct-array field.
        await IterateElementFilter(client);

        Console.WriteLine("Deleting...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = CollectionName });
        Console.WriteLine("Done.");
    }

    private static async Task IterateQuery(
        MilvusClientV2 client, int batch, int offset, int? limit, string filter, long rowCount)
    {
        #region Snippet:MilvusIterator_Query
        Console.WriteLine("=====================================================");
        Console.WriteLine($"Iterate batch: {batch} offset: {offset} limit: {limit?.ToString(CultureInfo.InvariantCulture) ?? "unlimited"} filter: {filter}");

        var request = new QueryIteratorReq
        {
            CollectionName = CollectionName,
            BatchSize = batch,
            Expression = filter,
            Parameters = new QueryParameters
            {
                Offset = offset,
                Limit = limit,
                // Strong pins a fresh snapshot so the verification below sees every row just inserted
                // (the collection default Bounded level may read a slightly stale view).
                ConsistencyLevel = ConsistencyLevel.Strong,
                OutputFields = { IdField, NameField, AgeField, "a" }
            }
        };

        var ids = new HashSet<long>();
        int pages = 0;
        long totalCount = 0;
        await foreach (IReadOnlyList<FieldData> page in client.QueryIteratorAsync(request))
        {
            int pageCount = page.FirstOrDefault()?.RowCount ?? 0;
            if (pageCount == 0)
            {
                Console.WriteLine("query iteration finished");
                break;
            }

            pages++;
            totalCount += pageCount;
            Console.WriteLine($"No.{pages} page {pageCount} rows fetched");
            Console.WriteLine($"\tthe first row: {ExampleHelpers.FormatRow(page, 0)}");
            Console.WriteLine($"\tthe last row: {ExampleHelpers.FormatRow(page, pageCount - 1)}");

            var idField = page.First(f => f.FieldName == IdField) as FieldData<long>;
            for (int i = 0; i < pageCount; i++)
            {
                ids.Add(idField!.Data[i]);
            }
        }
        #endregion

        // Verify the number of returned ids is expected; only check when the filter is empty, because
        // filtering is unpredictable.
        if (filter.Length == 0)
        {
            long expected = limit is null ? rowCount - offset : Math.Min(limit.Value, rowCount - offset);
            if (ids.Count != expected)
            {
                Console.WriteLine($"Returned row count is unexpected: {ids.Count} returned vs expected {expected}");
                Environment.Exit(1);
            }
        }

        Console.WriteLine($"Total fetched rows: {totalCount}");
        Console.WriteLine("=====================================================");
    }

    private static async Task IterateWithCursor(MilvusClientV2 client)
    {
        Console.WriteLine("=====================================================");
        QueryIterator iterator = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = CollectionName,
            BatchSize = 100,
            Parameters = new QueryParameters { Limit = 50, OutputFields = { NameField } }
        });

        int firstPageRows = 0;
        await foreach (IReadOnlyList<FieldData> page in iterator)
        {
            firstPageRows = page.FirstOrDefault()?.RowCount ?? 0;
            Console.WriteLine($"First page fetched {firstPageRows} rows.");
            break;
        }

        QueryIteratorCursor? cursor = iterator.GetCursor();
        if (cursor is null)
        {
            Console.WriteLine("Cursor is null; skipping the resume demo.");
            Console.WriteLine("=====================================================");
            return;
        }

        QueryIterator resumed = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = CollectionName,
            BatchSize = 100,
            Parameters = new QueryParameters { Limit = 50, OutputFields = { NameField } },
            Cursor = cursor
        });

        int resumedRows = 0;
        await foreach (IReadOnlyList<FieldData> page in resumed)
        {
            resumedRows = page.FirstOrDefault()?.RowCount ?? 0;
            Console.WriteLine($"Resumed iterator fetched {resumedRows} rows.");
            break;
        }

        Console.WriteLine("=====================================================");
    }

    private static async Task IterateElementFilter(MilvusClientV2 client)
    {
        Console.WriteLine("=====================================================");
        Console.WriteLine("Iterate element_filter(user_tags, $[tag] == \"news\")");

        var request = new QueryIteratorReq
        {
            CollectionName = CollectionName,
            BatchSize = 100,
            Expression = $"element_filter({TagsField}, $[tag] == \"news\")",
            Parameters = new QueryParameters { OutputFields = { IdField, TagsField } }
        };

        int pages = 0;
        long totalElements = 0;
        await foreach (IReadOnlyList<FieldData> page in client.QueryIteratorAsync(request))
        {
            int pageCount = page.FirstOrDefault()?.RowCount ?? 0;
            if (pageCount == 0)
            {
                Console.WriteLine("element-filter iteration finished");
                break;
            }

            pages++;
            totalElements += pageCount;
            Console.Write($"No.{pages} page {pageCount} matched elements: ");

            var idField = page.First(f => f.FieldName == IdField) as FieldData<long>;
            var tagsField = page.FirstOrDefault(f => f.FieldName == TagsField) as StructFieldData;
            var offsetField = page.First(f => f.FieldName == "offset") as FieldData<long>;
            for (int i = 0; i < pageCount; i++)
            {
                Console.Write($"[id={idField!.Data[i]} tags={JsonSerializer.Serialize(tagsField?.Data[i])} offset={offsetField!.Data[i]}] ");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"Total fetched matched elements: {totalElements}");
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
        // (in the sibling example) does not hit ties that the server cannot distinct.
        var random = new Random(unchecked((int)id));
        var vector = new float[Dimension];
        for (int d = 0; d < Dimension; d++)
        {
            vector[d] = (float)random.NextDouble();
        }

        return vector;
    }
}
