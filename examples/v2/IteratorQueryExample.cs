using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates the server-side query iterator: page over all rows of a collection in
/// <c>BatchSize</c>-sized batches via <c>await foreach</c>, capture and resume from a resumable
/// cursor, and iterate the matched elements of a struct-array field with an
/// <c>element_filter</c> expression. Mirrors cpp examples/src/v2/iterator_query.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show how to consume a large query result set without loading it at
/// once. The query iterator is primary-key-ordered and yields <c>IReadOnlyList&lt;FieldData&gt;</c>
/// batches; an <c>element_filter</c> expression over a struct-array field additionally expands
/// each matched entity into one row per matched element, with an <c>offset</c> column holding the
/// element positions.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>CreateIndexAsync</c>,
/// <c>InsertAsync</c>, <c>QueryIteratorAsync</c>, <c>QueryIterator.GetCursor()</c>,
/// <c>QueryIteratorReq.Cursor</c>, <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> a row count per query-iterator page, a resumed page count, an
/// element count per element-filter page, then "Done.".</para>
/// </remarks>
public static class IteratorQueryExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "iterator_query_example";
        const string idField = "id";
        const string nameField = "user_name";
        const string ageField = "user_age";
        const string clipsField = "user_clips";
        const string vectorField = "user_face";
        const int dimension = 4;

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);

        Console.WriteLine("Creating collection...");
        // The struct-array field user_clips carries a VarChar sub-field "tag" that the element_filter
        // demo below matches inside each struct element ($[tag]).
        var clipsFieldSchema = new StructFieldSchema(clipsField)
        {
            MaxCapacity = 10
        };
        clipsFieldSchema.Fields.Add(FieldSchema.CreateVarchar("tag", maxLength: 100));

        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema(idField, DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar(nameField, maxLength: 100),
                    new FieldSchema(ageField, DataType.Int8),
                    FieldSchema.CreateFloatVector(vectorField, dimension: dimension)
                },
                StructFields = { clipsFieldSchema }
            }
        });

        Console.WriteLine("Creating index...");
        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = collectionName,
            Indexes = [new IndexParam(vectorField, null, IndexType.Flat, MetricType.L2)]
        });

        // Insert a modest dataset; every third row also carries a struct element with tag "news"
        // (some twice), so the element_filter demo below has matching elements to iterate over.
        var rows = Enumerable.Range(0, 30)
            .Select(i =>
            {
                var clips = new List<object>
                {
                    new Dictionary<string, object?> { ["tag"] = "sports" }
                };
                if (i % 3 == 0)
                {
                    clips.Add(new Dictionary<string, object?> { ["tag"] = "news" });
                }

                if (i % 7 == 0)
                {
                    clips.Add(new Dictionary<string, object?> { ["tag"] = "news" });
                }

                return new Dictionary<string, object?>
                {
                    [idField] = (long)i,
                    [nameField] = $"user_{i}",
                    [ageField] = (sbyte)(i % 10),
                    [clipsField] = clips,
                    [vectorField] = new[] { i % 10 / 10f, i % 7 / 10f, i % 5 / 10f, i % 3 / 10f }
                };
            })
            .ToArray();

        Console.WriteLine("Inserting 30 rows...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            RowsData = rows
        });

        Console.WriteLine("Loading collection...");
        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = collectionName });

        #region Snippet:MilvusIteratorQuery_Query
        Console.WriteLine("Query iterator (batch size 10):");
        int queryRows = 0;
        await foreach (IReadOnlyList<FieldData> batch in client.QueryIteratorAsync(
            new QueryIteratorReq
            {
                CollectionName = collectionName,
                BatchSize = 10,
                Parameters = new QueryParameters { Limit = 50 }
            }))
        {
            Console.WriteLine($"  page of {batch.FirstOrDefault()?.RowCount ?? 0} rows");
            queryRows += batch.FirstOrDefault()?.RowCount ?? 0;
        }

        Console.WriteLine($"  total rows iterated: {queryRows}");
        #endregion

        #region Snippet:MilvusIteratorQuery_Filtered
        Console.WriteLine("Query iterator with filter and offset (batch size 10):");
        int filteredRows = 0;
        await foreach (IReadOnlyList<FieldData> batch in client.QueryIteratorAsync(
            new QueryIteratorReq
            {
                CollectionName = collectionName,
                BatchSize = 10,
                Expression = "id >= 10",
                Parameters = new QueryParameters { Limit = 40, Offset = 10 }
            }))
        {
            Console.WriteLine($"  page of {batch.FirstOrDefault()?.RowCount ?? 0} rows");
            filteredRows += batch.FirstOrDefault()?.RowCount ?? 0;
        }

        Console.WriteLine($"  total rows iterated with filter and offset: {filteredRows}");
        #endregion

        #region Snippet:MilvusIteratorQuery_Cursor
        Console.WriteLine("Query iterator with cursor capture and resume (batch size 5):");
        QueryIterator iterator = client.QueryIteratorAsync(new QueryIteratorReq
        {
            CollectionName = collectionName,
            BatchSize = 5,
            Parameters = new QueryParameters { Limit = 15 }
        });

        int firstRunRows = 0;
        await foreach (IReadOnlyList<FieldData> batch in iterator)
        {
            Console.WriteLine($"  page of {batch.FirstOrDefault()?.RowCount ?? 0} rows");
            firstRunRows += batch.FirstOrDefault()?.RowCount ?? 0;
        }

        Console.WriteLine($"  first run rows iterated: {firstRunRows}");

        // Capture the current position and resume a brand-new iterator from it (same MVCC snapshot).
        QueryIteratorCursor? cursor = iterator.GetCursor();
        if (cursor is not null)
        {
            Console.WriteLine("  resuming from captured cursor...");
            int resumedRows = 0;
            await foreach (IReadOnlyList<FieldData> batch in client.QueryIteratorAsync(new QueryIteratorReq
            {
                CollectionName = collectionName,
                BatchSize = 5,
                Parameters = new QueryParameters { Limit = 15 },
                Cursor = cursor
            }))
            {
                Console.WriteLine($"  page of {batch.FirstOrDefault()?.RowCount ?? 0} rows");
                resumedRows += batch.FirstOrDefault()?.RowCount ?? 0;
            }

            Console.WriteLine($"  resumed rows iterated: {resumedRows}");
        }
        #endregion

        #region Snippet:MilvusIteratorQuery_ElementFilter
        Console.WriteLine("Element-filter iterator (element_filter(user_clips, $[tag] == \"news\"), batch 10):");
        int elementCount = 0;
        await foreach (IReadOnlyList<FieldData> batch in client.QueryIteratorAsync(
            new QueryIteratorReq
            {
                CollectionName = collectionName,
                BatchSize = 10,
                Expression = "element_filter(user_clips, $[tag] == \"news\")",
                Parameters = new QueryParameters { OutputFields = { idField } }
            }))
        {
            int pageElements = batch.FirstOrDefault()?.RowCount ?? 0;
            Console.WriteLine($"  page of {pageElements} matched elements");
            elementCount += pageElements;

            // The element-filter iterator expands each entity into one row per matched element and appends
            // the element-offset column, so each page is one element per row.
            FieldData<long>? offsets = batch.FirstOrDefault(f => f.FieldName == "offset") as FieldData<long>;
            FieldData<long>? ids = batch.FirstOrDefault(f => f.FieldName == idField) as FieldData<long>;
            for (int i = 0; i < Math.Min(3, pageElements); i++)
            {
                Console.WriteLine($"    [offset={offsets?.Data[i]} id={ids?.Data[i]}]");
            }
        }

        Console.WriteLine($"  total matched elements iterated: {elementCount}");
        #endregion

        Console.WriteLine("Deleting...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        Console.WriteLine("Done.");
    }
}
