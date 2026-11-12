using System.Text.Json;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Index;
using Milvus.Client.V2.Responses.Dml;
using Milvus.Client.V2.Responses.Dql;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates Array-of-Struct fields end to end: a struct field with scalar, vector (float/binary/int8) and
/// nested Array sub-fields, element-level null, nullable struct rows, embedding-list and element-level search,
/// element-filter queries, adding a struct field to an existing collection, and evolving a struct field by
/// adding/dropping sub-fields.
/// Mirrors the Java SDK's <c>io.milvus.v2.StructExample</c> (same flow, field layout and data).
/// </summary>
public static class StructExample
{
    private const string CollectionName = "java_sdk_example_struct_v2";

    private const string IdField = "id";
    private const string NameField = "film_name";
    private const string StructField = "clips";
    private const string SimplifyStructField = "simplify_clips";
    private const string FrameField = "frame_number";
    private const string DescField = "clip_desc";
    private const string FloatVectorField = "clip_float_embedding";
    private const string BinaryVectorField = "clip_binary_embedding";
    private const string Int8VectorField = "clip_int8_embedding";
    private const string ExtraStructField = "metadata";
    private const string RatingField = "rating";
    private const string TagField = "tag";
    private const string ScoreField = "score";
    private const string LangField = "lang";
    private const string TagsField = "tags";
    private const string NoteField = "note";
    private const string MarksField = "marks";

    private const int FloatVectorDim = 128;
    private const int BinaryVectorDim = 32;
    private const int Int8VectorDim = 32;

    private const long ExtraRowId = 5000L;

    private static readonly Random Random = new();

    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        await CreateCollectionAsync(client);
        await InsertDataAsync(client, 2000);

        // Insert rows demonstrating element-level null and null struct rows.
        #region Snippet:MilvusStruct_Insert
        await client.InsertAsync(new InsertReq
        {
            CollectionName = CollectionName,
            RowsData = BuildElementNullRows()
        });
        #endregion
        await VerifyElementNullAsync(client);

        await VerifyNullableStructAsync(client);

        await SearchFloatVectorFieldAsync(client, $"{IdField} in [5, 8]");

        string filter1 = $"MATCH_LEAST({StructField}, $[{FrameField}] < 2000, threshold=3)";
        await SearchBinaryVectorFieldAsync(client, filter1);

        await SearchInt8VectorFieldAsync(client, $"{IdField} == 999");
        await ElementLevelSearchAsync(client);
        await ElementLevelQueryAsync(client);

        await AddCollectionStructFieldAsync(client);
        await EvolveStructSubFieldAsync(client);

        await ExampleHelpers.ResetCollectionAsync(client, CollectionName);
        Console.WriteLine("Collection dropped: " + CollectionName);
    }

    private static async Task CreateCollectionAsync(MilvusClientV2 client)
    {
        // A struct sub-field's Nullable enables element-level null (that sub-field may be null in a given
        // struct element); the struct's own Nullable enables whole-row null. The nested Array sub-field
        // "tags" uses ElementNullable for the values inside each inner array and MaxCapacity for the
        // inner-array capacity.
        var clips = new StructFieldSchema(StructField, "clips of a film")
        {
            MaxCapacity = 100,
            Nullable = true
        };
        clips.Fields.Add(new FieldSchema(FrameField, DataType.Int32, description: "from which frame this clip begin"));
        clips.Fields.Add(FieldSchema.CreateVarchar(DescField, maxLength: 1024, description: "description of a clip"));
        clips.Fields.Add(new FieldSchema(FloatVectorField, DataType.FloatVector, description: "float embedding of a clip")
        {
            Dimension = FloatVectorDim
        });
        clips.Fields.Add(new FieldSchema(BinaryVectorField, DataType.BinaryVector, description: "binary embedding of a clip")
        {
            Dimension = BinaryVectorDim
        });
        clips.Fields.Add(new FieldSchema(Int8VectorField, DataType.Int8Vector, description: "int8 embedding of a clip")
        {
            Dimension = Int8VectorDim
        });
        clips.Fields.Add(new FieldSchema(ScoreField, DataType.Int64, description: "score of a clip, element-level null allowed")
        {
            Nullable = true
        });
        clips.Fields.Add(FieldSchema.CreateVarchar(LangField, maxLength: 16,
            description: "language of a clip, element-level null allowed", nullable: true));
        clips.Fields.Add(new FieldSchema(TagsField, DataType.Array)
        {
            ElementDataType = DataType.VarChar,
            MaxCapacity = 8,
            MaxLength = 16,
            Nullable = true,
            ElementNullable = true
        });

        var simplifyClips = new StructFieldSchema(SimplifyStructField, "simplify clips")
        {
            MaxCapacity = 100,
            Fields =
            {
                new FieldSchema(FloatVectorField, DataType.FloatVector, description: "clip has been simplified")
                {
                    Dimension = 32
                }
            }
        };

        await ExampleHelpers.ResetCollectionAsync(client, CollectionName);

        #region Snippet:MilvusStruct_Create
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = CollectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema(IdField, DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateVarchar(NameField, maxLength: 1024)
                },
                StructFields = { clips, simplifyClips }
            }
        });

        await client.CreateIndexAsync(new CreateIndexReq
        {
            CollectionName = CollectionName,
            Indexes =
            [
                new IndexParam($"{StructField}[{FloatVectorField}]", "index_float", IndexType.Hnsw, MetricType.MaxSimIp),
                new IndexParam($"{StructField}[{BinaryVectorField}]", "index_binary", IndexType.Hnsw, MetricType.MaxSimHamming),
                new IndexParam($"{StructField}[{Int8VectorField}]", "index_int8", IndexType.Hnsw, MetricType.MaxSimL2),
                new IndexParam($"{SimplifyStructField}[{FloatVectorField}]", "index_simplify", IndexType.Hnsw, MetricType.L2)
            ]
        });

        await client.LoadCollectionAsync(new LoadCollectionReq { CollectionName = CollectionName });
        #endregion

        Console.WriteLine("Collection created: " + CollectionName);

        DescribeCollectionResp description = await client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = CollectionName });
        PrintStructSchema(description);
    }

    private static void PrintStructSchema(DescribeCollectionResp description)
    {
        foreach (StructFieldSchema structField in description.Schema.StructFields)
        {
            Console.WriteLine($"struct '{structField.Name}' nullable={structField.Nullable} maxCapacity={structField.MaxCapacity}");
            foreach (FieldSchema subField in structField.Fields)
            {
                Console.WriteLine($"  {subField.Name}: {subField.DataType}, isNullable={subField.Nullable}, elementNullable={subField.ElementNullable}");
            }
        }
    }

    private static Dictionary<string, object?> BuildBaseRow(long id, bool withClips)
    {
        var row = new Dictionary<string, object?>
        {
            [IdField] = id,
            [NameField] = $"film_{id}"
        };

        if (withClips)
        {
            var clips = new List<object>();
            for (int k = 0; k < 5; k++)
            {
                clips.Add(new Dictionary<string, object?>
                {
                    [FrameField] = Random.Next(10000),
                    [DescField] = $"clip_description_{id}",
                    [FloatVectorField] = GenerateFloatVector(FloatVectorDim),
                    [BinaryVectorField] = GenerateBinaryVector(BinaryVectorDim),
                    [Int8VectorField] = GenerateInt8Vector(Int8VectorDim),
                    [ScoreField] = (long)Random.Next(100),
                    [LangField] = "en",
                    [TagsField] = new object?[] { "a", "b" }
                });
            }

            row[StructField] = clips;
        }
        else
        {
            // The clips struct field is nullable; store a null row for it.
            row[StructField] = null;
        }

        var simplifyClips = new List<object>();
        for (int k = 0; k < 2; k++)
        {
            simplifyClips.Add(new Dictionary<string, object?> { [FloatVectorField] = GenerateFloatVector(32) });
        }

        row[SimplifyStructField] = simplifyClips;
        return row;
    }

    // Rows demonstrating element-level null: a missing or null sub-field key is stored as a null element for
    // the element-nullable sub-fields (score/lang); nested arrays support null inner values (tags: ["c", null])
    // and empty arrays; the whole struct row can be null (clips: null).
    private static List<Dictionary<string, object?>> BuildElementNullRows()
    {
        var row97 = new Dictionary<string, object?>
        {
            [IdField] = 97L,
            [NameField] = "film_97"
        };
        var c0 = new Dictionary<string, object?>
        {
            [FrameField] = 0,
            [DescField] = "clip_description_97",
            [FloatVectorField] = GenerateFloatVector(FloatVectorDim),
            [BinaryVectorField] = GenerateBinaryVector(BinaryVectorDim),
            [Int8VectorField] = GenerateInt8Vector(Int8VectorDim),
            [ScoreField] = 1L,
            [LangField] = "en",
            [TagsField] = new object?[] { "a", "b" }
        };
        var c1 = new Dictionary<string, object?>
        {
            [FrameField] = 1,
            [DescField] = "clip_description_97",
            [FloatVectorField] = GenerateFloatVector(FloatVectorDim),
            [BinaryVectorField] = GenerateBinaryVector(BinaryVectorDim),
            [Int8VectorField] = GenerateInt8Vector(Int8VectorDim),
            [ScoreField] = 2L,
            [LangField] = null, // element-level null for lang
            [TagsField] = new object?[] { "c", null } // null inner value in the nested array
        };
        var c2 = new Dictionary<string, object?>
        {
            [FrameField] = 2,
            [DescField] = "clip_description_97",
            [FloatVectorField] = GenerateFloatVector(FloatVectorDim),
            [BinaryVectorField] = GenerateBinaryVector(BinaryVectorDim),
            [Int8VectorField] = GenerateInt8Vector(Int8VectorDim),
            // score/lang omitted => both stored as null elements; tags is an empty array
            [TagsField] = Array.Empty<object?>()
        };
        row97[StructField] = new object[] { c0, c1, c2 };
        row97[SimplifyStructField] = Array.Empty<object>();

        var row98 = new Dictionary<string, object?>
        {
            [IdField] = 98L,
            [NameField] = "film_98",
            [StructField] = Array.Empty<object>(), // empty array, a non-null row
            [SimplifyStructField] = Array.Empty<object>()
        };

        var row99 = new Dictionary<string, object?>
        {
            [IdField] = 99L,
            [NameField] = "film_99",
            [StructField] = null, // null struct row
            [SimplifyStructField] = Array.Empty<object>()
        };

        return new List<Dictionary<string, object?>> { row97, row98, row99 };
    }

    // Query rows that carry element-level nulls and print how null elements / null struct rows are decoded back.
    private static async Task VerifyElementNullAsync(MilvusClientV2 client)
    {
        Console.WriteLine("===================================================");
        Console.WriteLine("Verify element-level null struct fields on rows 97/98/99");

        QueryResp queryResp = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = $"{IdField} in [97, 98, 99]",
            Parameters = new QueryParameters
            {
                OutputFields = { IdField, StructField },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        for (int i = 0; i < queryResp.RowCount; i++)
        {
            IReadOnlyDictionary<string, object?> row = queryResp.GetRow(i);
            long id = (long)row[IdField]!;
            object? clips = row[StructField];
            Console.WriteLine($"query result id={id}, clips={(clips is null ? "null" : JsonSerializer.Serialize(clips))}");
            if (id == 99L && clips is not null)
            {
                throw new InvalidOperationException($"Expected null clips for id=99, got: {JsonSerializer.Serialize(clips)}");
            }
        }

        Console.WriteLine("Element-level null verification passed");
    }

    private static async Task InsertDataAsync(MilvusClientV2 client, int rowCount)
    {
        const int batchSize = 300;
        int insertedCount = 0;
        while (insertedCount < rowCount)
        {
            int nextBatch = Math.Min(batchSize, rowCount - insertedCount);
            var rows = new List<Dictionary<string, object?>>();
            for (int i = 0; i < nextBatch; i++)
            {
                long id = insertedCount + i;
                // Insert null clips struct for every 100th row.
                rows.Add(BuildBaseRow(id, id % 100 != 0));
            }

            MutationResp insertResp = await client.InsertAsync(new InsertReq
            {
                CollectionName = CollectionName,
                RowsData = rows
            });
            insertedCount += (int)insertResp.InsertCount;
            Console.WriteLine("Inserted row count: " + insertResp.InsertCount);
        }

        QueryResp countR = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Parameters = new QueryParameters
            {
                OutputFields = { "count(*)" },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        Console.WriteLine($"{countR.GetRow(0)["count(*)"]} rows persisted");
    }

    private static async Task VerifyNullableStructAsync(MilvusClientV2 client)
    {
        Console.WriteLine("===================================================");
        Console.WriteLine($"Verify nullable struct field '{StructField}' returns null data");

        // Rows with id % 100 == 0 were inserted with clips = null.
        QueryResp queryResp = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = $"{IdField} in [0, 100, 200, 300]",
            Parameters = new QueryParameters
            {
                OutputFields = { IdField, NameField, StructField, SimplifyStructField },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        for (int i = 0; i < queryResp.RowCount; i++)
        {
            IReadOnlyDictionary<string, object?> row = queryResp.GetRow(i);
            long id = (long)row[IdField]!;
            object? clips = row[StructField];
            object? simplifyClips = row[SimplifyStructField];
            Console.WriteLine($"query result id={id}, clips={(clips is null ? "null" : JsonSerializer.Serialize(clips))}, " +
                $"simplify_clips.size={((IReadOnlyCollection<object?>)simplifyClips!).Count}");
            if (id % 100 == 0 && clips is not null)
            {
                throw new InvalidOperationException($"Expected null clips for id={id}, got: {JsonSerializer.Serialize(clips)}");
            }
        }

        // Take the simplify_clips vector of a null-clips row and search on it element-level; the top-1 hit
        // should be that row itself and its clips output must decode back to null.
        QueryResp vectorQuery = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = $"{IdField} == 100",
            Parameters = new QueryParameters
            {
                OutputFields = { SimplifyStructField },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        var simplifyStructs = (IReadOnlyList<IDictionary<string, object?>>)vectorQuery.GetRow(0)[SimplifyStructField]!;
        ReadOnlyMemory<float> queryVector = ToFloatVector(simplifyStructs[0][FloatVectorField]);

        SearchResp searchResp = await client.SearchAsync(new SearchReq
        {
            CollectionName = CollectionName,
            VectorFieldName = $"{SimplifyStructField}[{FloatVectorField}]",
            Vectors = new[] { queryVector },
            Limit = 5,
            MetricType = MetricType.L2,
            Parameters = new SearchParameters
            {
                OutputFields = { IdField, StructField },
                ConsistencyLevel = ConsistencyLevel.Bounded
            }
        });
        bool foundNullRow = false;
        SingleResult single = searchResp.SingleResults![0];
        for (int j = 0; j < single.Scores.Count; j++)
        {
            IReadOnlyDictionary<string, object?> row = single.GetRow(j);
            long id = (long)row[IdField]!;
            object? clips = row[StructField];
            Console.WriteLine($"search result id={id}, clips={(clips is null ? "null" : JsonSerializer.Serialize(clips))}");
            if (id == 100L)
            {
                if (clips is not null)
                {
                    throw new InvalidOperationException($"Expected null clips for id=100 in search result, got: {JsonSerializer.Serialize(clips)}");
                }

                foundNullRow = true;
            }
        }

        if (!foundNullRow)
        {
            throw new InvalidOperationException("The null-clips row id=100 was not returned by search");
        }

        Console.WriteLine("Nullable struct verification passed");
    }

    private static async Task<List<IReadOnlyDictionary<string, object?>>> QueryAsync(MilvusClientV2 client, string filter)
    {
        Console.WriteLine("===================================================");
        Console.WriteLine("Query with filter expression: " + filter);
        QueryResp queryResp = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = filter,
            Parameters = new QueryParameters
            {
                OutputFields = { StructField, SimplifyStructField },
                ConsistencyLevel = ConsistencyLevel.Bounded,
                Limit = 3
            }
        });

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        for (int i = 0; i < queryResp.RowCount; i++)
        {
            IReadOnlyDictionary<string, object?> row = queryResp.GetRow(i);
            Console.WriteLine(FormatRow(row));
            rows.Add(row);
        }

        return rows;
    }

    // Searches the struct vector sub-field with embedding lists (one list per row, matching each row's struct
    // elements against the query vectors and scoring the row by its closest element). Mirrors Java's
    // EmbeddingList search on the float sub-field.
    private static async Task SearchWithEmbeddingListsAsync(
        MilvusClientV2 client, string annsField, MetricType metricType,
        IReadOnlyList<EmbeddingList> searchData)
    {
        Console.WriteLine("===================================================");
        Console.WriteLine($"Search on field '{annsField}' in struct '{StructField}' with nq={searchData.Count}");

        SearchResp searchResp = await client.SearchAsync(new SearchReq
        {
            CollectionName = CollectionName,
            VectorFieldName = $"{StructField}[{annsField}]",
            EmbeddingLists = searchData,
            Limit = 5,
            MetricType = metricType,
            Parameters = new SearchParameters
            {
                OutputFields =
                {
                    NameField,
                    $"{StructField}[{FrameField}]",
                    $"{StructField}[{DescField}]",
                    $"{SimplifyStructField}[{FloatVectorField}]"
                },
                ConsistencyLevel = ConsistencyLevel.Bounded
            }
        });

        PrintSearchResults(searchResp, "embedding list");
    }

    private static async Task SearchFloatVectorFieldAsync(MilvusClientV2 client, string filter)
    {
        List<IReadOnlyDictionary<string, object?>> queryResults = await QueryAsync(client, filter);

        // Build one embedding list per row from the row's struct elements, skipping rows whose clips field is
        // null (a nullable struct row has no embeddings to search on).
        var searchData = new List<EmbeddingList>();
        foreach (IReadOnlyDictionary<string, object?> row in queryResults)
        {
            object? clips = row[StructField];
            if (clips is null)
            {
                continue;
            }

            var embList = new EmbeddingList();
            foreach (IDictionary<string, object?> structElement in (IEnumerable<IDictionary<string, object?>>)clips)
            {
                embList.Add(ToFloatVector(structElement[FloatVectorField]));
            }

            searchData.Add(embList);
        }

        await SearchWithEmbeddingListsAsync(client, FloatVectorField, MetricType.MaxSimIp, searchData);
    }

    private static async Task SearchBinaryVectorFieldAsync(MilvusClientV2 client, string filter)
    {
        List<IReadOnlyDictionary<string, object?>> queryResults = await QueryAsync(client, filter);

        // Build one embedding list per row from the row's binary struct vectors, skipping null struct rows.
        var searchData = new List<EmbeddingList>();
        foreach (IReadOnlyDictionary<string, object?> row in queryResults)
        {
            if (row[StructField] is not IEnumerable<IDictionary<string, object?>> clips)
            {
                continue;
            }

            var embList = new EmbeddingList();
            foreach (IDictionary<string, object?> structElement in clips)
            {
                embList.AddBinary(ToBinaryVector(structElement[BinaryVectorField]));
            }

            searchData.Add(embList);
        }

        await SearchWithEmbeddingListsAsync(client, BinaryVectorField, MetricType.MaxSimHamming, searchData);
    }

    private static async Task SearchInt8VectorFieldAsync(MilvusClientV2 client, string filter)
    {
        List<IReadOnlyDictionary<string, object?>> queryResults = await QueryAsync(client, filter);

        var searchData = new List<EmbeddingList>();
        foreach (IReadOnlyDictionary<string, object?> row in queryResults)
        {
            if (row[StructField] is not IEnumerable<IDictionary<string, object?>> clips)
            {
                continue;
            }

            var embList = new EmbeddingList();
            foreach (IDictionary<string, object?> structElement in clips)
            {
                embList.AddInt8(ToInt8Vector(structElement[Int8VectorField]));
            }

            searchData.Add(embList);
        }

        await SearchWithEmbeddingListsAsync(client, Int8VectorField, MetricType.MaxSimL2, searchData);
    }

    private static async Task ElementLevelSearchAsync(MilvusClientV2 client)
    {
        Console.WriteLine("===================================================");
        Console.WriteLine($"Element-level search on field '{FloatVectorField}' in struct '{SimplifyStructField}'");

        QueryResp queryResp = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = $"{IdField} == 5",
            Parameters = new QueryParameters
            {
                OutputFields = { SimplifyStructField },
                ConsistencyLevel = ConsistencyLevel.Bounded,
                Limit = 1
            }
        });
        var structs = (IReadOnlyList<IDictionary<string, object?>>)queryResp.GetRow(0)[SimplifyStructField]!;

        // Each row of simplify_clips contains two structs, and each struct has a clip_float_embedding, so we
        // have 2 vectors to search for this row.
        var elementSearchList = new List<ReadOnlyMemory<float>>();
        foreach (IDictionary<string, object?> structElement in structs)
        {
            elementSearchList.Add(ToFloatVector(structElement[FloatVectorField]));
        }

        SearchResp searchResp = await client.SearchAsync(new SearchReq
        {
            CollectionName = CollectionName,
            VectorFieldName = $"{SimplifyStructField}[{FloatVectorField}]",
            Vectors = elementSearchList,
            Limit = 3,
            MetricType = MetricType.L2,
            Parameters = new SearchParameters
            {
                OutputFields = { NameField },
                ConsistencyLevel = ConsistencyLevel.Bounded
            }
        });

        // There are two lists of search results corresponding to the two vectors in simplify_clips, and each
        // search result carries an element offset indicating which element in the struct-array field the
        // result corresponds to.
        int hitIndex = 0;
        for (int i = 0; i < searchResp.SingleResults!.Count; i++)
        {
            Console.WriteLine($"Results of No.{i} query element");
            SingleResult single = searchResp.SingleResults[i];
            for (int j = 0; j < single.Scores.Count; j++)
            {
                long? elementOffset = searchResp.ElementIndices is not null && hitIndex < searchResp.ElementIndices.Count
                    ? searchResp.ElementIndices[hitIndex]
                    : null;
                Console.WriteLine($"{FormatRow(single.GetRow(j))}, elementOffset={elementOffset}");
                hitIndex++;
            }
        }
    }

    private static async Task ElementLevelQueryAsync(MilvusClientV2 client)
    {
        Console.WriteLine("===================================================");
        string filter = $"element_filter({StructField}, $[{FrameField}] < 2000)";
        Console.WriteLine("Element-level query with filter expression: " + filter);

        // The server evaluates the filter at element granularity. The C# SDK's plain query returns one row per
        // matched entity (the element indices are consumed by the query-iterator element-filter path).
        QueryResp queryResp = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = filter,
            Parameters = new QueryParameters
            {
                OutputFields =
                {
                    IdField,
                    NameField,
                    $"{StructField}[{FrameField}]",
                    $"{StructField}[{DescField}]"
                },
                ConsistencyLevel = ConsistencyLevel.Bounded,
                Limit = 5
            }
        });
        for (int i = 0; i < queryResp.RowCount; i++)
        {
            Console.WriteLine(FormatRow(queryResp.GetRow(i)));
        }
    }

    private static async Task AddCollectionStructFieldAsync(MilvusClientV2 client)
    {
        Console.WriteLine("===================================================");
        Console.WriteLine("Add a new struct field to the existing collection");

        // A struct field added to an existing collection must be nullable.
        #region Snippet:MilvusStruct_AddStructField
        var extraStructField = new StructFieldSchema(ExtraStructField, "additional metadata for films")
        {
            MaxCapacity = 8,
            Nullable = true,
            Fields =
            {
                new FieldSchema(RatingField, DataType.Int32),
                FieldSchema.CreateVarchar(TagField, maxLength: 128)
            }
        };
        await client.AddCollectionStructFieldAsync(new AddCollectionStructFieldReq
        {
            CollectionName = CollectionName,
            StructField = extraStructField
        });
        #endregion
        Console.WriteLine("Added struct field: " + ExtraStructField);

        DescribeCollectionResp description = await client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = CollectionName });
        PrintStructSchema(description);

        QueryResp existingRow = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = $"{IdField} == 5",
            Parameters = new QueryParameters
            {
                OutputFields = { IdField, ExtraStructField },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        Console.WriteLine("Existing row after addCollectionStructField() - new field is null");
        for (int i = 0; i < existingRow.RowCount; i++)
        {
            Console.WriteLine(FormatRow(existingRow.GetRow(i)));
        }

        Dictionary<string, object?> newRow = BuildBaseRow(ExtraRowId, withClips: true);
        newRow[ExtraStructField] = new object[]
        {
            new Dictionary<string, object?> { [RatingField] = 5, [TagField] = "favorite" },
            new Dictionary<string, object?> { [RatingField] = 4, [TagField] = "classic" }
        };
        await client.InsertAsync(new InsertReq
        {
            CollectionName = CollectionName,
            RowsData = new[] { newRow }
        });

        QueryResp newRowResp = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = $"{IdField} == {ExtraRowId}",
            Parameters = new QueryParameters
            {
                OutputFields = { IdField, NameField, ExtraStructField },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        Console.WriteLine("New row with the added struct field");
        for (int i = 0; i < newRowResp.RowCount; i++)
        {
            Console.WriteLine(FormatRow(newRowResp.GetRow(i)));
        }

        QueryResp projectedResp = await client.QueryAsync(new QueryReq
        {
            CollectionName = CollectionName,
            Expression = $"{IdField} == {ExtraRowId}",
            Parameters = new QueryParameters
            {
                OutputFields =
                {
                    IdField,
                    NameField,
                    $"{ExtraStructField}[{RatingField}]",
                    $"{ExtraStructField}[{TagField}]"
                },
                ConsistencyLevel = ConsistencyLevel.Strong
            }
        });
        Console.WriteLine("Projected subfields from the added struct field");
        for (int i = 0; i < projectedResp.RowCount; i++)
        {
            Console.WriteLine(FormatRow(projectedResp.GetRow(i)));
        }
    }

    // Evolve an existing struct field: add sub-fields (including a nested Array sub-field), insert rows
    // carrying them, and drop one again. Sub-field add/drop use the bracketed `struct[sub]` path.
    private static async Task EvolveStructSubFieldAsync(MilvusClientV2 client)
    {
        Console.WriteLine("===================================================");
        Console.WriteLine($"Evolve struct field '{StructField}' by adding sub-fields");

        await client.AddCollectionFieldAsync(new AddCollectionFieldReq
        {
            CollectionName = CollectionName,
            Field = new FieldSchema($"{StructField}[{NoteField}]", DataType.VarChar)
            {
                MaxLength = 64,
                Nullable = true
            }
        });
        Console.WriteLine($"Added struct sub-field {StructField}[{NoteField}]");

        await client.AddCollectionFieldAsync(new AddCollectionFieldReq
        {
            CollectionName = CollectionName,
            Field = new FieldSchema($"{StructField}[{MarksField}]", DataType.Array)
            {
                ElementDataType = DataType.Int32,
                MaxCapacity = 4,
                Nullable = true,
                ElementNullable = true
            }
        });
        Console.WriteLine($"Added nested Array struct sub-field {StructField}[{MarksField}]");

        var row = new Dictionary<string, object?>
        {
            [IdField] = 6000L,
            [NameField] = "film_6000"
        };
        var c0 = new Dictionary<string, object?>
        {
            [FrameField] = 0,
            [DescField] = "clip_description_6000",
            [FloatVectorField] = GenerateFloatVector(FloatVectorDim),
            [BinaryVectorField] = GenerateBinaryVector(BinaryVectorDim),
            [Int8VectorField] = GenerateInt8Vector(Int8VectorDim),
            [ScoreField] = 7L,
            [NoteField] = "hello",
            [MarksField] = new object?[] { 1, 2 }
        };
        row[StructField] = new object[] { c0 };
        row[SimplifyStructField] = Array.Empty<object>();
        await client.InsertAsync(new InsertReq
        {
            CollectionName = CollectionName,
            RowsData = new[] { row }
        });

        DescribeCollectionResp described = await client.DescribeCollectionAsync(
            new DescribeCollectionReq { CollectionName = CollectionName });
        Console.WriteLine("Collection fields after sub-field evolution:");
        foreach (StructFieldSchema structField in described.Schema.StructFields)
        {
            if (structField.Name == StructField)
            {
                foreach (FieldSchema subField in structField.Fields)
                {
                    Console.WriteLine($"  {subField.Name}: {subField.DataType}, isNullable={subField.Nullable}, elementNullable={subField.ElementNullable}");
                }
            }
        }

        await client.DropCollectionFieldAsync(new DropCollectionFieldReq
        {
            CollectionName = CollectionName,
            FieldName = $"{StructField}[{NoteField}]"
        });
        Console.WriteLine($"Dropped struct sub-field {StructField}[{NoteField}]");
    }

    private static float[] GenerateFloatVector(int dim)
    {
        var vector = new float[dim];
        for (int i = 0; i < dim; i++)
        {
            vector[i] = (float)(Random.NextDouble() * 2 - 1);
        }

        return vector;
    }

    // Query results return struct vectors as ReadOnlyMemory<T> (insert input accepts T[]); accept both.
    private static ReadOnlyMemory<float> ToFloatVector(object? value)
        => value switch
        {
            ReadOnlyMemory<float> memory => memory,
            float[] array => array.AsMemory(),
            _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name}' to a float vector")
        };

    private static ReadOnlyMemory<byte> ToBinaryVector(object? value)
        => value switch
        {
            ReadOnlyMemory<byte> memory => memory,
            byte[] array => array.AsMemory(),
            _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name}' to a binary vector")
        };

    private static ReadOnlyMemory<sbyte> ToInt8Vector(object? value)
        => value switch
        {
            ReadOnlyMemory<sbyte> memory => memory,
            sbyte[] array => array.AsMemory(),
            _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name}' to an int8 vector")
        };

    private static byte[] GenerateBinaryVector(int dim)
    {
        var bytes = new byte[dim / 8];
        Random.NextBytes(bytes);
        return bytes;
    }

    private static sbyte[] GenerateInt8Vector(int dim)
    {
        var vector = new sbyte[dim];
        for (int i = 0; i < dim; i++)
        {
            vector[i] = (sbyte)Random.Next(sbyte.MinValue, sbyte.MaxValue + 1);
        }

        return vector;
    }

    private static string FormatRow(IReadOnlyDictionary<string, object?> row)
        => string.Join(", ", row.Select(pair => $"{pair.Key}={FormatValue(pair.Value)}"));

    private static void PrintSearchResults(SearchResp searchResp, string resultLabel)
    {
        if (searchResp.SingleResults is null)
        {
            return;
        }

        for (int i = 0; i < searchResp.SingleResults.Count; i++)
        {
            Console.WriteLine($"Results of No.{i} {resultLabel}");
            SingleResult single = searchResp.SingleResults[i];
            for (int j = 0; j < single.Scores.Count; j++)
            {
                Console.WriteLine(FormatRow(single.GetRow(j)));
            }
        }
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is ReadOnlyMemory<float> floatVector)
        {
            return $"[{string.Join(", ", floatVector.ToArray())}]";
        }

        if (value is ReadOnlyMemory<byte> byteVector)
        {
            return $"[{string.Join(", ", byteVector.ToArray())}]";
        }

        if (value is ReadOnlyMemory<sbyte> sbyteVector)
        {
            return $"[{string.Join(", ", sbyteVector.ToArray())}]";
        }

        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            return JsonSerializer.Serialize(value);
        }

        return value.ToString() ?? "null";
    }
}
