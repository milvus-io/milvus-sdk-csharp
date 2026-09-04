using Google.Protobuf.Collections;
using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// Shared conversions between proto and V2 DTOs for the DQL domain (search/query results).
/// </summary>
internal static class DqlConversions
{
    /// <summary>
    /// Converts the proto field data of a search/query result into V2 <see cref="FieldData" /> objects.
    /// </summary>
    public static List<FieldData> ProcessReturnedFieldData(RepeatedField<Grpc.FieldData> grpcFields)
    {
        var results = new List<FieldData>(grpcFields.Count);
        foreach (Grpc.FieldData grpcField in grpcFields)
        {
            if (grpcField.IsDynamic)
            {
                // Surface dynamic fields as their raw JSON metadata (the server-side "$meta" column)
                // instead of silently dropping them. The aggregated field carries no schema-level name.
                if (grpcField.Scalars?.DataCase == Grpc.ScalarField.DataOneofCase.JsonData)
                {
                    results.Add(FieldData.CreateDynamicJson(
                        grpcField.Scalars.JsonData.Data.Select(p => p.ToStringUtf8()).ToList()));
                }

                continue;
            }

            results.Add(FromGrpcFieldData(grpcField));
        }

        return results;
    }

    // The group-by search response carries the per-group field value in a FieldData whose FieldName is
    // empty on the wire; FieldData.Create requires a non-blank name, so synthesize one. The synthesized
    // name is only a local label; callers access the value through the response's GroupByFieldValue.
    private const string GroupByFieldValueName = "group_by_field_value";

    internal static FieldData? ProcessGroupByFieldValue(Grpc.FieldData groupByFieldValue)
    {
        if (groupByFieldValue.FieldName is { Length: > 0 })
        {
            return FromGrpcFieldData(groupByFieldValue);
        }

        // FieldName has a public setter on the generated message; copy the value into a fresh message so
        // we do not mutate the server's response object.
        Grpc.FieldData renamed = groupByFieldValue.Clone();
        renamed.FieldName = GroupByFieldValueName;
        return FromGrpcFieldData(renamed);
    }

    /// <summary>
    /// Trims each column of the given field-data list to the rows in <c>[start, start+count)</c>, preserving
    /// each field's name, data type and dynamic flag. Used by the query iterator to cap a page that the server
    /// over-delivers when <c>reduce_stop_for_best</c> is enabled, and by <c>SingleResult</c> slicing.
    /// </summary>
    public static IReadOnlyList<FieldData> TakeRows(IReadOnlyList<FieldData> fields, int start, int count)
    {
        if (fields.Count == 0)
        {
            return fields;
        }

        var result = new List<FieldData>(fields.Count);
        foreach (FieldData field in fields)
        {
            result.Add(field.Slice(start, count));
        }

        return result;
    }

    /// <summary>
    /// Decodes the server-reported <c>report_value</c> (mutation cost in milliseconds) from a status's
    /// <c>extra_info</c> map, returning 0 when absent or unparseable. Mirrors the Java SDK's
    /// <c>getCost</c> (and PyMilvus's <c>get_cost_from_status</c>).
    /// </summary>
    public static long GetReportValue(Grpc.Status? status)
        => status is null ? 0L : GetExtraInfoLong(status, "report_value");

    /// <summary>
    /// Decodes a <see cref="long" /> value from a status's <c>extra_info</c> map, returning 0 when absent or
    /// unparseable. Used for the search metrics the server reports alongside results (scanned bytes).
    /// </summary>
    public static long GetExtraInfoLong(Grpc.Status? status, string key)
    {
        if (status is null || !status.ExtraInfo.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            return 0L;
        }

        return long.TryParse(value.Trim(), System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out long parsed) ? parsed : 0L;
    }

    /// <summary>
    /// Decodes a <see cref="float" /> value from a status's <c>extra_info</c> map, returning <c>null</c> when
    /// absent or unparseable. Used for the search cache-hit ratio the server reports alongside results.
    /// </summary>
    public static float? GetExtraInfoFloat(Grpc.Status? status, string key)
    {
        if (status is null || !status.ExtraInfo.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return float.TryParse(value.Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float parsed) ? parsed : null;
    }

    private static FieldData FromGrpcFieldData(Grpc.FieldData fieldData)
    {
        switch (fieldData.FieldCase)
        {
            case Grpc.FieldData.FieldOneofCase.Vectors:
                return ConvertVectors(fieldData);

            case Grpc.FieldData.FieldOneofCase.Scalars:
                bool nullable = fieldData.ValidData.Count > 0;
                return fieldData.Scalars.DataCase switch
                {
                    Grpc.ScalarField.DataOneofCase.BoolData => nullable
                        ? FieldData.Create(fieldData.FieldName, ExpandNullable(fieldData.Scalars.BoolData.Data.Select(x => (bool?)x).ToList(), fieldData.ValidData, null))
                        : FieldData.Create(fieldData.FieldName, fieldData.Scalars.BoolData.Data),
                    Grpc.ScalarField.DataOneofCase.IntData => ConvertIntData(fieldData),
                    Grpc.ScalarField.DataOneofCase.LongData => nullable
                        ? FieldData.Create(fieldData.FieldName, ExpandNullable(fieldData.Scalars.LongData.Data.Select(x => (long?)x).ToList(), fieldData.ValidData, null))
                        : FieldData.Create(fieldData.FieldName, fieldData.Scalars.LongData.Data),
                    Grpc.ScalarField.DataOneofCase.FloatData => nullable
                        ? FieldData.Create(fieldData.FieldName, ExpandNullable(fieldData.Scalars.FloatData.Data.Select(x => (float?)x).ToList(), fieldData.ValidData, null))
                        : FieldData.Create(fieldData.FieldName, fieldData.Scalars.FloatData.Data),
                    Grpc.ScalarField.DataOneofCase.DoubleData => nullable
                        ? FieldData.Create(fieldData.FieldName, ExpandNullable(fieldData.Scalars.DoubleData.Data.Select(x => (double?)x).ToList(), fieldData.ValidData, null))
                        : FieldData.Create(fieldData.FieldName, fieldData.Scalars.DoubleData.Data),
                    Grpc.ScalarField.DataOneofCase.StringData => ConvertStringData(fieldData),
                    Grpc.ScalarField.DataOneofCase.TimestamptzData => ConvertTimestamptzData(fieldData),
                    Grpc.ScalarField.DataOneofCase.JsonData => ConvertJsonData(fieldData),
                    Grpc.ScalarField.DataOneofCase.ArrayData => ConvertArray(fieldData),
                    // GeometryWktData (WKT strings) round-trips as a Geometry-typed string FieldData so a
                    // query-then-reinsert keeps the geometry type (the StringData slot is rejected by the
                    // proxy for geometry fields). Expand nulls against ValidData like the scalar branches.
                    Grpc.ScalarField.DataOneofCase.GeometryWktData => new FieldData<string?>(
                        fieldData.FieldName,
                        nullable
                            ? ExpandNullable(fieldData.Scalars.GeometryWktData.Data.ToList(), fieldData.ValidData, null)
                            : fieldData.Scalars.GeometryWktData.Data.ToList(),
                        DataType.Geometry),
                    // GeometryData (binary WKB) and BytesData decode to byte-vector rows.
                    Grpc.ScalarField.DataOneofCase.GeometryData => new FieldData<byte[]>(
                        fieldData.FieldName, fieldData.Scalars.GeometryData.Data.Select(b => b.ToByteArray()).ToList(), DataType.Geometry),
                    Grpc.ScalarField.DataOneofCase.BytesData => new FieldData<byte[]>(
                        fieldData.FieldName, fieldData.Scalars.BytesData.Data.Select(b => b.ToByteArray()).ToList(), DataType.BinaryVector),
                    _ => throw new NotSupportedException($"{fieldData.Scalars.DataCase} not supported")
                };

            case Grpc.FieldData.FieldOneofCase.StructArrays:
                return ConvertStruct(fieldData);

            default:
                throw new NotSupportedException($"{fieldData.FieldCase} not supported");
        }
    }

    // Rebuilds the logical row sequence from the server's field data plus the per-row valid_data mask.
    // Milvus sends scalar (and array) columns positionally: one data slot per row (zero/empty-filled for
    // null rows) with a same-length valid_data mask, while only vector columns advance a compacted cursor
    // (valid-only values) against the same mask. The two encodings are told apart by length: when the data
    // and mask lengths match the column is positional; otherwise it is compacted and the false positions
    // need null placeholders re-inserted. This matches the Java SDK's FieldDataWrapper.setNoneData, which
    // masks positionally only when validData.size() == data.size().
    private static IReadOnlyList<T> ExpandNullable<T>(
        IReadOnlyList<T> data, IReadOnlyList<bool> validData, T nullValue)
    {
        if (validData.Count == 0)
        {
            return data;
        }

        bool positional = validData.Count == data.Count;
        var expanded = new List<T>(validData.Count);
        int dataIndex = 0;
        for (int i = 0; i < validData.Count; i++)
        {
            if (validData[i])
            {
                expanded.Add(positional ? data[i] : data[dataIndex++]);
            }
            else
            {
                expanded.Add(nullValue);
            }
        }

        return expanded;
    }

    // The server encodes Int8/Int16/Int32 scalar fields through the same IntData (int32) array; branch on the
    // field's declared type so a decoded Int8/Int16/Int32 column round-trips with its original DataType instead
    // of being widened to Int64 (matching the V1 SDK's behavior).
    private static FieldData ConvertIntData(Grpc.FieldData fieldData)
    {
        IReadOnlyList<int> data = fieldData.Scalars.IntData.Data;
        bool nullable = fieldData.ValidData.Count > 0;
        return (fieldData.Type, nullable) switch
        {
            (Grpc.DataType.Int8, false) => FieldData.Create(fieldData.FieldName, data.Select(x => (sbyte)x).ToList()),
            (Grpc.DataType.Int8, true) => FieldData.Create(fieldData.FieldName, ExpandNullable(data.Select(x => (sbyte?)x).ToList(), fieldData.ValidData, null)),
            (Grpc.DataType.Int16, false) => FieldData.Create(fieldData.FieldName, data.Select(x => (short)x).ToList()),
            (Grpc.DataType.Int16, true) => FieldData.Create(fieldData.FieldName, ExpandNullable(data.Select(x => (short?)x).ToList(), fieldData.ValidData, null)),
            (Grpc.DataType.Int32, false) => FieldData.Create(fieldData.FieldName, data),
            (Grpc.DataType.Int32, true) => FieldData.Create(fieldData.FieldName, ExpandNullable(data.Select(x => (int?)x).ToList(), fieldData.ValidData, null)),
            _ => nullable
                ? FieldData.Create(fieldData.FieldName, ExpandNullable(data.Select(x => (long?)x).ToList(), fieldData.ValidData, null))
                : FieldData.Create(fieldData.FieldName, data.Select(x => (long)x).ToList())
        };
    }

    // Decodes string-slot scalar columns. Timestamptz fields travel as ISO-8601 strings in the string_data
    // slot (the proxy's timestamptzUTC2IsoStr rewrite); decode them with their real data type rather than
    // widening to VarChar, mirroring the V1 decoder which branches on Grpc.DataType.Timestamptz.
    private static FieldData<string> ConvertStringData(Grpc.FieldData fieldData)
    {
        bool nullable = fieldData.ValidData.Count > 0;
        bool isTimestamptz = fieldData.Type == Grpc.DataType.Timestamptz;
        IReadOnlyList<string> data = fieldData.Scalars.StringData.Data;
        if (!nullable)
        {
            return isTimestamptz
                ? new FieldData<string>(fieldData.FieldName, data, DataType.Timestamptz)
                : FieldData.CreateVarChar(fieldData.FieldName, data);
        }

        // A nullable string column expands to a List<string?>; the string generic parameter carries the
        // reference-type nulls in the same list, so cast to IReadOnlyList<string> for the DTO constructor.
        IReadOnlyList<string?> expanded = ExpandNullable(
            data.Select(x => (string?)x).ToList(), fieldData.ValidData, null);
        return isTimestamptz
            ? new FieldData<string>(fieldData.FieldName, (IReadOnlyList<string>)(object)expanded, DataType.Timestamptz)
            : new FieldData<string>(fieldData.FieldName, (IReadOnlyList<string>)(object)expanded);
    }

    // Decodes the native TimestamptzArray slot (int64 epoch microseconds) when a server emits it, converting
    // to the ISO-8601 string representation used everywhere else in the SDK, instead of falling through to
    // NotSupportedException. The proxy's timestamptzUTC2IsoStr uses time.UnixMicro, so the slot is in
    // microseconds; DateTimeOffset.FromUnixTimeMicroseconds is unavailable on netstandard2.0, so divide to
    // milliseconds (losing sub-millisecond precision).
    private static FieldData<string> ConvertTimestamptzData(Grpc.FieldData fieldData)
    {
        IReadOnlyList<string> data = fieldData.Scalars.TimestamptzData.Data
            .Select(ts => DateTimeOffset.FromUnixTimeMilliseconds(ts / 1000).ToString("O", System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        return new FieldData<string>(fieldData.FieldName, data, DataType.Timestamptz);
    }

    // Decodes a JSON column, expanding nullable rows back into place via valid_data (mirroring the string
    // branch). A nullable JSON column with any null row has the nulls compacted out of the JSON array on the
    // wire, so they must be re-inserted here to keep the row count aligned.
    private static FieldData<string> ConvertJsonData(Grpc.FieldData fieldData)
    {
        bool nullable = fieldData.ValidData.Count > 0;
        IReadOnlyList<string> data = fieldData.Scalars.JsonData.Data.Select(p => p.ToStringUtf8()).ToList();
        if (!nullable)
        {
            return FieldData.CreateJson(fieldData.FieldName, data);
        }

        IReadOnlyList<string?> expanded = ExpandNullable(
            data.Select(x => (string?)x).ToList(), fieldData.ValidData, null);
        // Preserve DataType.Json (the public FieldData<string> ctor would infer VarChar from string).
        return new FieldData<string>(fieldData.FieldName, (IReadOnlyList<string>)(object)expanded, DataType.Json);
    }

    private static FieldData ConvertVectors(Grpc.FieldData fieldData)
    {
        Grpc.VectorField vectors = fieldData.Vectors;
        FieldData result = vectors.DataCase switch
        {
            Grpc.VectorField.DataOneofCase.FloatVector
                => FieldData.CreateFloatVector(fieldData.FieldName,
                    ExpandNullable(ChunkFloats(vectors.FloatVector.Data, (int)vectors.Dim), fieldData.ValidData, ReadOnlyMemory<float>.Empty)),

            Grpc.VectorField.DataOneofCase.Float16Vector => ConvertFloat16Vectors(fieldData, vectors),

            Grpc.VectorField.DataOneofCase.Bfloat16Vector => ConvertBFloat16Vectors(fieldData, vectors),

            Grpc.VectorField.DataOneofCase.Int8Vector => ConvertInt8Vectors(fieldData, vectors),

            Grpc.VectorField.DataOneofCase.BinaryVector => ConvertBinaryVectors(fieldData, vectors),

            Grpc.VectorField.DataOneofCase.SparseFloatVector => ConvertSparseVectors(fieldData, vectors),

            _ => throw new NotSupportedException($"VectorField.DataOneofCase.{vectors.DataCase} not supported")
        };

        // Carry ValidData forward so a decoded nullable vector column fed back into InsertAsync/UpsertAsync
        // keeps its null rows (the placeholder expansion above only fixes the row count/dim, not validity).
        if (fieldData.ValidData.Count > 0)
        {
            result.SetValidData(fieldData.ValidData.ToList());
        }

        return result;
    }

    private static ReadOnlyMemory<float>[] ChunkFloats(RepeatedField<float> data, int dim)
    {
        if (dim <= 0)
        {
            return data.Count == 0 ? Array.Empty<ReadOnlyMemory<float>>() : new ReadOnlyMemory<float>[] { data.ToArray() };
        }

        int vectorCount = data.Count / dim;
        var vectors = new ReadOnlyMemory<float>[vectorCount];
        for (int i = 0; i < vectorCount; i++)
        {
            var vector = new float[dim];
            for (int j = 0; j < dim; j++)
            {
                vector[j] = data[i * dim + j];
            }
            vectors[i] = vector;
        }

        return vectors;
    }

    private static BinaryVectorFieldData ConvertBinaryVectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        int dim = (int)vectors.Dim;
        if (dim <= 0)
        {
            return FieldData.CreateBinaryVectors(fieldData.FieldName,
                ExpandNullable(Array.Empty<ReadOnlyMemory<byte>>(), fieldData.ValidData, ReadOnlyMemory<byte>.Empty));
        }

        // Ceiling-divide by 8 so a dimension in 1..7 still yields 1 byte per vector instead of a zero divisor.
        int bytesPerVector = (dim + 7) / 8;
        byte[] raw = vectors.BinaryVector.ToByteArray();
        var rows = new ReadOnlyMemory<byte>[raw.Length / bytesPerVector];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = raw.AsMemory(i * bytesPerVector, bytesPerVector);
        }

        return FieldData.CreateBinaryVectors(fieldData.FieldName,
            ExpandNullable(rows, fieldData.ValidData, ReadOnlyMemory<byte>.Empty));
    }

    private static Float16VectorFieldData ConvertFloat16Vectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        int dim = (int)vectors.Dim;
        if (dim <= 0)
        {
            return new Float16VectorFieldData(fieldData.FieldName,
                ExpandNullable(Array.Empty<ReadOnlyMemory<ushort>>(), fieldData.ValidData, ReadOnlyMemory<ushort>.Empty));
        }

        byte[] raw = vectors.Float16Vector.ToByteArray();
        var rows = new ReadOnlyMemory<ushort>[raw.Length / (dim * 2)];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new ushort[dim];
            int offset = i * dim * 2;
            for (int j = 0; j < dim; j++)
            {
                row[j] = (ushort)(raw[offset + j * 2] | (raw[offset + j * 2 + 1] << 8));
            }

            rows[i] = row;
        }

        return new Float16VectorFieldData(fieldData.FieldName,
            ExpandNullable(rows, fieldData.ValidData, ReadOnlyMemory<ushort>.Empty));
    }

    private static BFloat16VectorFieldData ConvertBFloat16Vectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        int dim = (int)vectors.Dim;
        if (dim <= 0)
        {
            return new BFloat16VectorFieldData(fieldData.FieldName,
                ExpandNullable(Array.Empty<ReadOnlyMemory<ushort>>(), fieldData.ValidData, ReadOnlyMemory<ushort>.Empty));
        }

        byte[] raw = vectors.Bfloat16Vector.ToByteArray();
        var rows = new ReadOnlyMemory<ushort>[raw.Length / (dim * 2)];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new ushort[dim];
            int offset = i * dim * 2;
            for (int j = 0; j < dim; j++)
            {
                row[j] = (ushort)(raw[offset + j * 2] | (raw[offset + j * 2 + 1] << 8));
            }

            rows[i] = row;
        }

        return new BFloat16VectorFieldData(fieldData.FieldName,
            ExpandNullable(rows, fieldData.ValidData, ReadOnlyMemory<ushort>.Empty));
    }

    private static Int8VectorFieldData ConvertInt8Vectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        int dim = (int)vectors.Dim;
        if (dim <= 0)
        {
            return new Int8VectorFieldData(fieldData.FieldName,
                ExpandNullable(Array.Empty<ReadOnlyMemory<sbyte>>(), fieldData.ValidData, ReadOnlyMemory<sbyte>.Empty));
        }

        byte[] raw = vectors.Int8Vector.ToByteArray();
        var rows = new ReadOnlyMemory<sbyte>[raw.Length / dim];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new sbyte[dim];
            for (int j = 0; j < dim; j++)
            {
                row[j] = unchecked((sbyte)raw[i * dim + j]);
            }

            rows[i] = row;
        }

        return new Int8VectorFieldData(fieldData.FieldName,
            ExpandNullable(rows, fieldData.ValidData, ReadOnlyMemory<sbyte>.Empty));
    }

    private static SparseFloatVectorFieldData ConvertSparseVectors(Grpc.FieldData fieldData, Grpc.VectorField vectors)
    {
        var sparseVectors = new MilvusSparseVector<float>[vectors.SparseFloatVector.Contents.Count];
        for (int i = 0; i < sparseVectors.Length; i++)
        {
            sparseVectors[i] = MilvusSparseVector<float>.FromBytes(vectors.SparseFloatVector.Contents[i].Span);
        }

        return FieldData.CreateSparseFloatVector(fieldData.FieldName,
            ExpandNullable(sparseVectors, fieldData.ValidData, default(MilvusSparseVector<float>)));
    }

    private static FieldData ConvertArray(Grpc.FieldData fieldData)
    {
        Grpc.ArrayArray arrayData = fieldData.Scalars.ArrayData;
        return arrayData.ElementType switch
        {
            Grpc.DataType.Bool => ConvertArrayElements<bool>(fieldData, arrayData, x => x.BoolData?.Data ?? []),
            Grpc.DataType.Int8 => ConvertArrayElements<sbyte>(fieldData, arrayData, x => x.IntData?.Data.Select(v => (sbyte)v) ?? []),
            Grpc.DataType.Int16 => ConvertArrayElements<short>(fieldData, arrayData, x => x.IntData?.Data.Select(v => (short)v) ?? []),
            Grpc.DataType.Int32 => ConvertArrayElements<int>(fieldData, arrayData, x => x.IntData?.Data ?? []),
            Grpc.DataType.Int64 => ConvertArrayElements<long>(fieldData, arrayData, x => x.LongData?.Data ?? []),
            Grpc.DataType.Float => ConvertArrayElements<float>(fieldData, arrayData, x => x.FloatData?.Data ?? []),
            Grpc.DataType.Double => ConvertArrayElements<double>(fieldData, arrayData, x => x.DoubleData?.Data ?? []),
            Grpc.DataType.String or Grpc.DataType.VarChar => ConvertArrayElements<string>(fieldData, arrayData, x => x.StringData?.Data ?? []),
            _ => throw new NotSupportedException($"Array element type {arrayData.ElementType} not supported")
        };
    }

    // Converts an Array column into one FieldData row per entity, expanding the compact arrayData.Data against
    // ValidData so null array rows keep the field row-aligned with its sibling fields (like the scalar/vector
    // branches). A null row is emitted for each invalid position. The concrete ArrayFieldData<TElement> type
    // preserves the element type (Int8/Int16 stay narrow instead of widening to Int32).
    private static ArrayFieldData<T> ConvertArrayElements<T>(
        Grpc.FieldData fieldData, Grpc.ArrayArray arrayData, Func<Grpc.ScalarField, IEnumerable<T>> selector)
    {
        List<IReadOnlyList<T>> rows = arrayData.Data
            .Select(x => (IReadOnlyList<T>)selector(x).ToList())
            .ToList();

        IReadOnlyList<IReadOnlyList<T>?> expanded =
            (IReadOnlyList<IReadOnlyList<T>?>)(object)ExpandNullable(rows, fieldData.ValidData, null);
        return new ArrayFieldData<T>(fieldData.FieldName, expanded);
    }

    // Decodes a struct field (proto FieldData.type = ArrayOfStruct with a StructArrayField payload) into a
    // StructFieldData whose rows are lists of { sub-field-name -> value } dictionaries, matching the Java
    // FieldDataWrapper.getStructData and the C++ ConvertStructFieldData shapes.
    private static StructFieldData ConvertStruct(Grpc.FieldData fieldData)
    {
        var subFields = new List<FieldSchema>();
        var subColumns = new List<Func<int, IDictionary<string, object?>>>(); // rowIndex -> per-row dict values

        foreach (Grpc.FieldData subField in fieldData.StructArrays.Fields)
        {
            DataType dataType = subField.Type == Grpc.DataType.ArrayOfVector
                ? VectorElementType(subField)
                : subField.Scalars.ArrayData.ElementType == Grpc.DataType.None
                    ? (DataType)subField.Type
                    : (DataType)subField.Scalars.ArrayData.ElementType;

            var subFieldSchema = new FieldSchema(subField.FieldName, dataType);
            subFields.Add(subFieldSchema);

            if (subField.Type == Grpc.DataType.ArrayOfVector)
            {
                subColumns.Add(CreateStructVectorReader(subField, subFieldSchema));
            }
            else
            {
                subColumns.Add(CreateStructScalarReader(subField, subFieldSchema));
            }
        }

        int rowCount = fieldData.StructArrays.Fields.Count == 0
            ? 0
            : fieldData.StructArrays.Fields[0].Type == Grpc.DataType.ArrayOfVector
                ? fieldData.StructArrays.Fields[0].Vectors.VectorArray.Data.Count
                : fieldData.StructArrays.Fields[0].Scalars.ArrayData.Data.Count;

        // A nullable struct column carries per-row valid_data flags; rows flagged false decode as null.
        bool[]? validData = fieldData.ValidData.Count == rowCount
            ? fieldData.ValidData.Select(v => v).ToArray()
            : null;

        var rows = new IReadOnlyList<IDictionary<string, object?>>?[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            if (validData is not null && !validData[i])
            {
                rows[i] = null;
                continue;
            }

            var structs = new List<IDictionary<string, object?>>();
            int elementCount = 0;
            var rowValues = new Dictionary<string, IReadOnlyList<object?>>();
            foreach (Func<int, IDictionary<string, object?>> reader in subColumns)
            {
                IDictionary<string, object?> values = reader(i);
                string name = values.Keys.First();
                var list = (IReadOnlyList<object?>)values[name]!;
                rowValues[name] = list;
                elementCount = Math.Max(elementCount, list.Count);
            }

            for (int k = 0; k < elementCount; k++)
            {
                var structDict = new Dictionary<string, object?>();
                foreach (KeyValuePair<string, IReadOnlyList<object?>> pair in rowValues)
                {
                    structDict[pair.Key] = pair.Value.Count > k ? pair.Value[k] : null;
                }

                structs.Add(structDict);
            }

            rows[i] = structs;
        }

        return new StructFieldData(fieldData.FieldName, rows, subFields);
    }

    private static DataType VectorElementType(Grpc.FieldData subField)
        => (DataType)subField.Vectors.VectorArray.ElementType;

    // Builds a reader returning the k-th row's list of struct-element values for a scalar sub-field
    // (each ArrayArray.data item is one row's packed scalar values for the sub-field).
    private static Func<int, IDictionary<string, object?>> CreateStructScalarReader(
        Grpc.FieldData subField, FieldSchema schema)
    {
        Grpc.ArrayArray arrayData = subField.Scalars.ArrayData;
        Grpc.DataType elementType = arrayData.ElementType;
        var rows = new IReadOnlyList<object?>[arrayData.Data.Count];
        for (int i = 0; i < arrayData.Data.Count; i++)
        {
            rows[i] = DecodeStructScalarRow(arrayData.Data[i], elementType);
        }

        string name = subField.FieldName;
        return row => new Dictionary<string, object?> { [name] = rows[row] };
    }

    private static List<object?> DecodeStructScalarRow(Grpc.ScalarField scalar, Grpc.DataType elementType)
        => elementType switch
        {
            // A struct element with no data (oneof DataCase None) yields an empty row; guard the typed accessors
            // so an empty/null struct element cannot throw a NullReferenceException during decode.
            Grpc.DataType.Bool => (scalar.BoolData?.Data ?? []).Select(x => (object?)x).ToList(),
            Grpc.DataType.Int8 => (scalar.IntData?.Data ?? []).Select(x => (object?)(sbyte)x).ToList(),
            Grpc.DataType.Int16 => (scalar.IntData?.Data ?? []).Select(x => (object?)(short)x).ToList(),
            Grpc.DataType.Int32 => (scalar.IntData?.Data ?? []).Select(x => (object?)x).ToList(),
            Grpc.DataType.Int64 => (scalar.LongData?.Data ?? []).Select(x => (object?)x).ToList(),
            Grpc.DataType.Float => (scalar.FloatData?.Data ?? []).Select(x => (object?)x).ToList(),
            Grpc.DataType.Double => (scalar.DoubleData?.Data ?? []).Select(x => (object?)x).ToList(),
            Grpc.DataType.VarChar or Grpc.DataType.String => (scalar.StringData?.Data ?? []).Select(x => (object?)x).ToList(),
            Grpc.DataType.Json => (scalar.JsonData?.Data ?? []).Select(p => (object?)p.ToStringUtf8()).ToList(),
            _ => throw new NotSupportedException($"Struct scalar sub-field element type {elementType} not supported")
        };

    // Builds a reader returning the k-th row's list of struct-element vectors for a vector sub-field
    // (each VectorArray.data item is one row's packed vectors for the sub-field).
    private static Func<int, IDictionary<string, object?>> CreateStructVectorReader(
        Grpc.FieldData subField, FieldSchema schema)
    {
        Grpc.VectorArray vectorArray = subField.Vectors.VectorArray;
        Grpc.DataType elementType = vectorArray.ElementType;
        // The outer VectorField.dim carries the schema dimension (Java sets it via
        // VectorField.newBuilder().setVectorArray(...).setDim(schemaDim)); VectorArray.dim may be zero.
        int dim = subField.Vectors.Dim != 0 ? (int)subField.Vectors.Dim : (int)vectorArray.Dim;
        string name = subField.FieldName;
        if (dim <= 0)
        {
            // No dimensions means no decodable vectors; degrade to an empty row list instead of a
            // divide-by-zero in the chunking helpers below.
            return _ => new Dictionary<string, object?> { [name] = Array.Empty<object?>() };
        }

        var rows = new IReadOnlyList<object?>[vectorArray.Data.Count];
        for (int i = 0; i < vectorArray.Data.Count; i++)
        {
            rows[i] = DecodeStructVectorRow(vectorArray.Data[i], elementType, dim);
        }

        return row => new Dictionary<string, object?> { [name] = rows[row] };
    }

    private static List<object?> DecodeStructVectorRow(
        Grpc.VectorField vector, Grpc.DataType elementType, int dim)
        => elementType switch
        {
            // An empty struct-vector element (oneof DataCase None) yields an empty row; guard the typed
            // accessors so a null struct vector element cannot throw during decode.
            Grpc.DataType.FloatVector => ChunkFloats(vector.FloatVector?.Data ?? [], dim).Select(x => (object?)x).ToList(),
            Grpc.DataType.BinaryVector => ChunkBytes(vector.BinaryVector?.ToByteArray() ?? [], Math.Max(1, (dim + 7) / 8)).Select(x => (object?)x).ToList(),
            Grpc.DataType.Float16Vector => ChunkUshort(vector.Float16Vector?.ToByteArray() ?? [], dim).Select(x => (object?)x).ToList(),
            Grpc.DataType.Bfloat16Vector => ChunkUshort(vector.Bfloat16Vector?.ToByteArray() ?? [], dim).Select(x => (object?)x).ToList(),
            Grpc.DataType.Int8Vector => ChunkSbyte(vector.Int8Vector?.ToByteArray() ?? [], dim).Select(x => (object?)x).ToList(),
            Grpc.DataType.SparseFloatVector => (vector.SparseFloatVector?.Contents ?? [])
                .Select(c => (object?)MilvusSparseVector<float>.FromBytes(c.Span)).ToList(),
            _ => throw new NotSupportedException($"Struct vector sub-field element type {elementType} not supported")
        };

    private static ReadOnlyMemory<byte>[] ChunkBytes(byte[] raw, int bytesPerVector)
    {
        var rows = new ReadOnlyMemory<byte>[raw.Length / bytesPerVector];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = raw.AsMemory(i * bytesPerVector, bytesPerVector);
        }

        return rows;
    }

    private static ReadOnlyMemory<ushort>[] ChunkUshort(byte[] raw, int dim)
    {
        var rows = new ReadOnlyMemory<ushort>[raw.Length / (dim * 2)];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new ushort[dim];
            int offset = i * dim * 2;
            for (int j = 0; j < dim; j++)
            {
                row[j] = (ushort)(raw[offset + j * 2] | (raw[offset + j * 2 + 1] << 8));
            }

            rows[i] = row;
        }

        return rows;
    }

    private static ReadOnlyMemory<sbyte>[] ChunkSbyte(byte[] raw, int dim)
    {
        var rows = new ReadOnlyMemory<sbyte>[raw.Length / dim];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new sbyte[dim];
            for (int j = 0; j < dim; j++)
            {
                row[j] = unchecked((sbyte)raw[i * dim + j]);
            }

            rows[i] = row;
        }

        return rows;
    }
}
