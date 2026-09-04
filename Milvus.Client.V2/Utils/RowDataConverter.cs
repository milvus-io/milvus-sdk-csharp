using System.Collections;
using System.Globalization;
using System.Text.Json;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Utils;

// Converts row-based insert/upsert data (a list of per-row dictionaries) into columnar FieldData, mirroring
// the C++ SDK's CheckAndSetRowData / CheckAndSetFieldValue. Values are encoded according to the collection
// schema's field types; rows may omit non-input fields (auto-id primary keys on insert, nullable fields with
// defaults), and with dynamic fields enabled, unknown keys are aggregated into the $meta JSON column.
internal static class RowDataConverter
{
    internal static List<FieldData> ConvertRows(
        IReadOnlyList<IDictionary<string, object?>> rows,
        CollectionSchema schema,
        bool isUpsert,
        bool isPartialUpdate = false)
    {
        int rowCount = rows.Count;

        var fieldNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (FieldSchema field in schema.Fields)
        {
            fieldNames.Add(field.Name);
        }

        var structFields = new Dictionary<string, StructFieldSchema>(StringComparer.Ordinal);
        foreach (StructFieldSchema structField in schema.StructFields)
        {
            structFields[structField.Name] = structField;
        }

        var columns = new Dictionary<string, List<object?>>(StringComparer.Ordinal);
        var structColumns = new Dictionary<string, List<object?>>(StringComparer.Ordinal);
        string[]? dynamicJson = schema.EnableDynamicFields ? new string[rowCount] : null;

        for (int i = 0; i < rowCount; i++)
        {
            IDictionary<string, object?> row = rows[i];
            if (row is null)
            {
                throw new ArgumentException($"Row {i} is null; each row must be a non-null dictionary.", nameof(rows));
            }

            foreach (KeyValuePair<string, object?> pair in row)
            {
                string name = pair.Key;
                object? value = pair.Value;

                if (structFields.ContainsKey(name))
                {
                    if (!structColumns.TryGetValue(name, out List<object?>? structColumn))
                    {
                        structColumns[name] = structColumn = new List<object?>(new object?[rowCount]);
                    }

                    structColumn[i] = value;
                    continue;
                }

                if (!fieldNames.Contains(name))
                {
                    if (!schema.EnableDynamicFields)
                    {
                        throw new ArgumentException($"Row {i} contains unknown field '{name}' and dynamic fields " +
                            "are not enabled on the collection.", nameof(rows));
                    }

                    dynamicJson![i] = MergeDynamicKey(dynamicJson[i] ?? "{}", name, value);
                    continue;
                }

                if (!columns.TryGetValue(name, out List<object?>? column))
                {
                    columns[name] = column = new List<object?>(new object?[rowCount]);
                }

                column[i] = value;
            }
        }

        var result = new List<FieldData>(columns.Count + structColumns.Count + (dynamicJson is null ? 0 : 1));

        foreach (FieldSchema field in schema.Fields)
        {
            // Function output fields are filled by the server; never accept client values for them.
            if (field.IsFunctionOutput)
            {
                continue;
            }

            if (columns.TryGetValue(field.Name, out List<object?>? column))
            {
                // Auto-id primary key values are forwarded for the server to validate (insert may provide them
                // when allow_insert_auto_id is enabled; upsert always provides them).
                result.Add(BuildColumn(field, column));
            }
            else if (isUpsert || IsInputRequired(field))
            {
                if (isPartialUpdate)
                {
                    // A partial upsert only carries the fields the caller supplied; every other field is left
                    // unchanged by the server, so omitted fields are skipped rather than required or defaulted.
                    continue;
                }

                if (field.Nullable)
                {
                    // Nullable-with-default fields may be omitted; build a column of nulls so the wire carries
                    // valid_data=false and the server applies the default.
                    result.Add(BuildColumn(field, new List<object?>(new object?[rowCount])));
                }
                else if (field.DefaultValue is not null)
                {
                    // Non-nullable fields with a default may also be omitted; mirror the Java/C++ SDKs and fill
                    // the default client-side so the row is inserted rather than rejected.
                    List<object?> defaultValues = new(rowCount);
                    for (int i = 0; i < rowCount; i++)
                    {
                        defaultValues.Add(field.DefaultValue);
                    }

                    result.Add(BuildColumn(field, defaultValues));
                }
                else if (!isUpsert && field.IsPrimaryKey && field.AutoId)
                {
                    // Auto-id primary key is not required on insert.
                }
                else
                {
                    throw new ArgumentException(
                        $"Field '{field.Name}' is not provided and has no default value or nullable flag.",
                        nameof(rows));
                }
            }
        }

        if (dynamicJson is not null)
        {
            // A row with only schema fields gets an empty JSON object; the server accepts it.
            for (int i = 0; i < rowCount; i++)
            {
                dynamicJson[i] ??= "{}";
            }

            result.Add(FieldData.CreateDynamicJson(dynamicJson));
        }

        // Struct fields: build one StructFieldData per provided struct column, converting each row's value
        // (a list of struct-element dictionaries) into the columnar shape.
        foreach (StructFieldSchema structField in schema.StructFields)
        {
            if (!structColumns.TryGetValue(structField.Name, out List<object?>? structColumn))
            {
                continue;
            }

            var rowsData = new IReadOnlyList<IDictionary<string, object?>>?[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                rowsData[i] = structColumn[i] is IEnumerable enumerable
                    ? enumerable.Cast<object?>().Select(AsStructElement).ToList()
                    : structColumn[i] is null ? null : throw new ArgumentException(
                        $"Row {i} of struct field '{structField.Name}' is not a list of struct elements.",
                        nameof(rows));
            }

            result.Add(new StructFieldData(structField.Name, rowsData, structField.Fields.ToList()));
        }

        return result;
    }

    private static IDictionary<string, object?> AsStructElement(object? value)
    {
        if (value is IDictionary<string, object?> dict)
        {
            return dict;
        }

        if (value is IDictionary<string, object> objDict)
        {
            var result = new Dictionary<string, object?>();
            foreach (KeyValuePair<string, object> pair in objDict)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        throw new ArgumentException(
            value is null
                ? "A struct element cannot be null."
                : $"A struct element must be a dictionary, but got '{value.GetType().Name}'.");
    }

    private static bool IsInputRequired(FieldSchema field)
        => !field.IsPrimaryKey || !field.AutoId;

    private static string MergeDynamicKey(string json, string key, object? value)
    {
        JsonDocument document = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                property.WriteTo(writer);
            }

            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, value);
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static FieldData BuildColumn(FieldSchema field, List<object?> values)
    {
        switch (field.DataType)
        {
            case DataType.Bool:
                return field.Nullable
                    ? FieldData.Create(field.Name, values.Select(v => v is null ? (bool?)null : ConvertBool(v)).ToList(), field.IsDynamic)
                    : FieldData.Create(field.Name, values.Select(v => RejectNull(v, field.Name) is null ? false : ConvertBool(v)).ToList(), field.IsDynamic);
            case DataType.Int8:
                return field.Nullable
                    ? FieldData.Create(field.Name, values.Select(v => v is null ? (sbyte?)null : checked((sbyte)Convert.ToInt32(v, CultureInfo.InvariantCulture))).ToList(), field.IsDynamic)
                    : FieldData.Create(field.Name, values.Select(v => { RejectNull(v, field.Name); return checked((sbyte)Convert.ToInt32(v, CultureInfo.InvariantCulture)); }).ToList(), field.IsDynamic);
            case DataType.Int16:
                return field.Nullable
                    ? FieldData.Create(field.Name, values.Select(v => v is null ? (short?)null : Convert.ToInt16(v, CultureInfo.InvariantCulture)).ToList(), field.IsDynamic)
                    : FieldData.Create(field.Name, values.Select(v => { RejectNull(v, field.Name); return Convert.ToInt16(v, CultureInfo.InvariantCulture); }).ToList(), field.IsDynamic);
            case DataType.Int32:
                return field.Nullable
                    ? FieldData.Create(field.Name, values.Select(v => v is null ? (int?)null : Convert.ToInt32(v, CultureInfo.InvariantCulture)).ToList(), field.IsDynamic)
                    : FieldData.Create(field.Name, values.Select(v => { RejectNull(v, field.Name); return Convert.ToInt32(v, CultureInfo.InvariantCulture); }).ToList(), field.IsDynamic);
            case DataType.Int64:
                return field.Nullable
                    ? FieldData.Create(field.Name, values.Select(v => v is null ? (long?)null : Convert.ToInt64(v, CultureInfo.InvariantCulture)).ToList(), field.IsDynamic)
                    : FieldData.Create(field.Name, values.Select(v => { RejectNull(v, field.Name); return Convert.ToInt64(v, CultureInfo.InvariantCulture); }).ToList(), field.IsDynamic);
            case DataType.Float:
                return field.Nullable
                    ? FieldData.Create(field.Name, values.Select(v => v is null ? (float?)null : Convert.ToSingle(v, CultureInfo.InvariantCulture)).ToList(), field.IsDynamic)
                    : FieldData.Create(field.Name, values.Select(v => { RejectNull(v, field.Name); return Convert.ToSingle(v, CultureInfo.InvariantCulture); }).ToList(), field.IsDynamic);
            case DataType.Double:
                return field.Nullable
                    ? FieldData.Create(field.Name, values.Select(v => v is null ? (double?)null : Convert.ToDouble(v, CultureInfo.InvariantCulture)).ToList(), field.IsDynamic)
                    : FieldData.Create(field.Name, values.Select(v => { RejectNull(v, field.Name); return Convert.ToDouble(v, CultureInfo.InvariantCulture); }).ToList(), field.IsDynamic);
            case DataType.VarChar:
            case DataType.String:
                return field.Nullable
                    ? FieldData.CreateVarChar(
                        field.Name,
                        values.Select(v => v is null ? null : Convert.ToString(v, CultureInfo.InvariantCulture)).ToList()!,
                        field.IsDynamic)
                    : FieldData.CreateVarChar(
                        field.Name,
                        values.Select(v => Convert.ToString(RejectNull(v, field.Name), CultureInfo.InvariantCulture)).ToList()!,
                        field.IsDynamic);
            case DataType.Json:
                // A null on a nullable JSON column becomes a C# null element so the encoder emits
                // valid_data=false (the server applies the default instead of storing an explicit JSON null).
                // On a non-nullable column a null value is rejected.
                return field.Nullable
                    ? FieldData.CreateJson(field.Name, values.Select(v => v is null ? null : EncodeJson(v)).ToList()!, field.IsDynamic)
                    : FieldData.CreateJson(field.Name, values.Select(v => EncodeJson(RejectNull(v, field.Name))).ToList(), field.IsDynamic);
            case DataType.Timestamptz:
                // Timestamptz row values travel as ISO-8601 strings in the string_data slot (Java sends the
                // same shape); accept a string, or a DateTimeOffset/DateTime serialized to ISO-8601.
                return field.Nullable
                    ? FieldData.CreateVarChar(field.Name, values.Select(ToTimestamptzStringOrNull).ToList()!, field.IsDynamic)
                    : FieldData.CreateVarChar(field.Name, values.Select(v => ToTimestamptzString(RejectNull(v, field.Name), field.Name)).ToList()!, field.IsDynamic);
            case DataType.Geometry:
                // Geometry row values travel as GeoJSON/WKT strings in the dedicated geometry_wkt_data slot
                // (the StringData slot is rejected by the proxy for geometry fields), mirroring the Java SDK's
                // ParamUtils geometry branch. The field is not nullable, so a null value is rejected like the
                // other scalar branches.
                return new FieldData<string>(
                    field.Name,
                    values.Select(v => Convert.ToString(RejectNull(v, field.Name), CultureInfo.InvariantCulture)).ToList()!,
                    DataType.Geometry,
                    field.IsDynamic);
            case DataType.FloatVector:
                return BuildFloatVector(field, values);
            case DataType.BinaryVector:
                return BuildBinaryVector(field, values);
            case DataType.SparseFloatVector:
                return BuildSparseVector(field, values);
            case DataType.Float16Vector:
            case DataType.BFloat16Vector:
                return BuildFloat16Vector(field, values);
            case DataType.Int8Vector:
                return BuildInt8Vector(field, values);
            case DataType.Array:
                return BuildArray(field, values);
            default:
                throw new NotSupportedException(
                    $"Row-based insert does not support data type {field.DataType} for field '{field.Name}'.");
        }
    }

    private static bool ConvertBool(object? value)
        => value switch
        {
            bool b => b,
            string s => bool.TryParse(s, out bool parsed) ? parsed : Convert.ToBoolean(s, CultureInfo.InvariantCulture),
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
        };

    // Rejects an explicit null on a non-nullable column with a clear error instead of silently coercing it to
    // a default (Convert.ToInt64(null) == 0, ConvertBool(null) == false). Returns the value so it can be used
    // in a Select chain.
    private static object? RejectNull(object? value, string fieldName)
        => value is null
            ? throw new ArgumentException(
                $"Field '{fieldName}' is not nullable but a row provides a null value.", nameof(value))
            : value;

    private static string EncodeJson(object? value)
        => value is null ? "null" : JsonSerializer.Serialize(value);

    // Timestamptz row values travel as ISO-8601 strings (matching the Java SDK); accept a pre-formatted
    // string or a DateTimeOffset/DateTime and render it in round-trip ISO-8601. For a DateTime, new
    // DateTimeOffset(dt) preserves the instant by applying the local offset (a Kind=Local value keeps its
    // wall clock tagged with the host's offset; Kind=Unspecified is treated as Local), avoiding the
    // SpecifyKind-Utc pitfall that would relabel a local time as +00:00 without converting it.
    private static string ToTimestamptzString(object? value, string fieldName)
        => value switch
        {
            string s => s,
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateTime dt => new DateTimeOffset(dt).ToString("O", CultureInfo.InvariantCulture),
            _ => throw new ArgumentException(
                $"Field '{fieldName}' expects an ISO-8601 string or DateTimeOffset value for Timestamptz, got '{value!.GetType().Name}'.")
        };

    private static string? ToTimestamptzStringOrNull(object? value)
        => value is null ? null : ToTimestamptzString(value, "");

    private static FloatVectorFieldData BuildFloatVector(FieldSchema field, List<object?> values)
    {
        int? dim = field.Dimension;
        var validData = new bool[values.Count];
        var rows = new ReadOnlyMemory<float>[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] is null)
            {
                validData[i] = false;
                rows[i] = ReadOnlyMemory<float>.Empty;
                continue;
            }

            validData[i] = true;
            float[] arr = ToFloatArray(values[i]!);
            if (dim is { } d && arr.Length != d)
            {
                throw new ArgumentException(
                    $"Row {i} of float vector field '{field.Name}' has dimension {arr.Length}, expected {d}.");
            }

            rows[i] = arr;
        }

        var result = new FloatVectorFieldData(field.Name, rows) { ValidData = values.Any(v => v is null) ? validData : null };
        return result;
    }

    private static float[] ToFloatArray(object value)
        => value switch
        {
            ReadOnlyMemory<float> memory => memory.ToArray(),
            float[] array => array,
            IEnumerable<float> enumerable => enumerable.ToArray(),
            IEnumerable<object> objects => objects.Select(o => Convert.ToSingle(o, CultureInfo.InvariantCulture)).ToArray(),
            IList list => list.Cast<object>().Select(o => Convert.ToSingle(o, CultureInfo.InvariantCulture)).ToArray(),
            _ => throw new ArgumentException($"Cannot convert '{value.GetType().Name}' to a float vector.")
        };

    private static BinaryVectorFieldData BuildBinaryVector(FieldSchema field, List<object?> values)
    {
        var validData = new bool[values.Count];
        var rows = new ReadOnlyMemory<byte>[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] is null)
            {
                validData[i] = false;
                rows[i] = ReadOnlyMemory<byte>.Empty;
                continue;
            }

            validData[i] = true;
            rows[i] = ToByteMemory(values[i]!);
        }

        return new BinaryVectorFieldData(field.Name, rows) { ValidData = values.Any(v => v is null) ? validData : null };
    }

    private static ReadOnlyMemory<byte> ToByteMemory(object value)
        => value switch
        {
            ReadOnlyMemory<byte> memory => memory,
            byte[] array => array,
            IEnumerable<byte> enumerable => enumerable.ToArray(),
            string hex => ParseHex(hex),
            _ => throw new ArgumentException($"Cannot convert '{value.GetType().Name}' to a binary vector.")
        };

    private static byte[] ParseHex(string hex)
    {
        if ((hex.Length & 1) != 0)
        {
            throw new ArgumentException($"Binary vector hex string '{hex}' has an odd length.");
        }

        var result = new byte[hex.Length / 2];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = (byte)((HexNibble(hex[i * 2]) << 4) | HexNibble(hex[(i * 2) + 1]));
        }

        return result;
    }

    private static int HexNibble(char c)
        => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => throw new ArgumentException($"Invalid hex digit '{c}' in binary vector string.")
        };

    private static SparseFloatVectorFieldData BuildSparseVector(FieldSchema field, List<object?> values)
    {
        var validData = new bool[values.Count];
        var rows = new MilvusSparseVector<float>[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] is null)
            {
                validData[i] = false;
                rows[i] = new MilvusSparseVector<float>(Array.Empty<int>(), Array.Empty<float>());
                continue;
            }

            validData[i] = true;
            rows[i] = ToSparseVector(values[i]!);
        }

        return new SparseFloatVectorFieldData(field.Name, rows) { ValidData = values.Any(v => v is null) ? validData : null };
    }

    private static MilvusSparseVector<float> ToSparseVector(object value)
    {
        if (value is MilvusSparseVector<float> sparse)
        {
            return sparse;
        }

        var pairs = new List<KeyValuePair<int, float>>();
        foreach (KeyValuePair<object, object?> pair in value is IDictionary<object, object?> dict
            ? dict
            : value is IDictionary<long, float> longFloatDict ? CastDictionary(longFloatDict)
            : value is IDictionary<int, float> intFloatDict ? CastDictionary(intFloatDict)
            : value is IDictionary<string, float> strFloatDict ? CastStringDictionary(strFloatDict)
            : value is IDictionary<string, double> strDoubleDict ? CastStringDictionary(strDoubleDict)
            : value is IDictionary<string, int> strIntDict ? CastStringDictionary(strIntDict)
            : value is IDictionary<string, long> strLongDict ? CastStringDictionary(strLongDict)
            : value is IDictionary<string, object> strObjDict ? CastStringDictionary(strObjDict)
            : value is IDictionary<int, double> intDoubleDict ? CastDictionary(intDoubleDict)
            : value is IDictionary<int, long> intLongDict ? CastDictionary(intLongDict)
            : value is IDictionary<long, double> longDoubleDict ? CastDictionary(longDoubleDict)
            : throw new ArgumentException($"Cannot convert '{value.GetType().Name}' to a sparse vector."))
        {
            pairs.Add(new KeyValuePair<int, float>(
                Convert.ToInt32(pair.Key, CultureInfo.InvariantCulture),
                Convert.ToSingle(pair.Value, CultureInfo.InvariantCulture)));
        }

        // MilvusSparseVector requires strictly ascending indices; dictionary enumeration order (insertion or
        // bucket order) is not guaranteed to be sorted, so sort by index before constructing.
        pairs.Sort((a, b) => a.Key.CompareTo(b.Key));
        var indices = new int[pairs.Count];
        var sparseValues = new float[pairs.Count];
        for (int i = 0; i < pairs.Count; i++)
        {
            indices[i] = pairs[i].Key;
            sparseValues[i] = pairs[i].Value;
        }

        return new MilvusSparseVector<float>(indices, sparseValues);
    }

    private static IEnumerable<KeyValuePair<object, object?>> CastDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
        where TKey : notnull
    {
        foreach (KeyValuePair<TKey, TValue> pair in dict)
        {
            yield return new KeyValuePair<object, object?>(pair.Key, pair.Value);
        }
    }

    private static IEnumerable<KeyValuePair<object, object?>> CastStringDictionary<TValue>(IDictionary<string, TValue> dict)
    {
        foreach (KeyValuePair<string, TValue> pair in dict)
        {
            // Convert the string index eagerly so a non-numeric sparse index fails with a clear error instead
            // of a raw FormatException from Convert.ToInt32 downstream.
            yield return new KeyValuePair<object, object?>(
                int.TryParse(pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                    ? parsed
                    : throw new ArgumentException(
                        $"Sparse vector index '{pair.Key}' is not a numeric integer."),
                pair.Value);
        }
    }

    private static FieldData BuildFloat16Vector(FieldSchema field, List<object?> values)
    {
        var validData = new bool[values.Count];
        var rows = new ReadOnlyMemory<ushort>[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] is null)
            {
                validData[i] = false;
                rows[i] = ReadOnlyMemory<ushort>.Empty;
                continue;
            }

            validData[i] = true;
            rows[i] = ToUshortMemory(values[i]!);
        }

        return field.DataType == DataType.Float16Vector
            ? new Float16VectorFieldData(field.Name, rows) { ValidData = values.Any(v => v is null) ? validData : null }
            : new BFloat16VectorFieldData(field.Name, rows) { ValidData = values.Any(v => v is null) ? validData : null };
    }

    private static ReadOnlyMemory<ushort> ToUshortMemory(object value)
        => value switch
        {
            ReadOnlyMemory<ushort> memory => memory,
            ushort[] array => array,
            IEnumerable<ushort> enumerable => enumerable.ToArray(),
            _ => throw new ArgumentException($"Cannot convert '{value.GetType().Name}' to a float16 vector.")
        };

    private static Int8VectorFieldData BuildInt8Vector(FieldSchema field, List<object?> values)
    {
        var validData = new bool[values.Count];
        var rows = new ReadOnlyMemory<sbyte>[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] is null)
            {
                validData[i] = false;
                rows[i] = ReadOnlyMemory<sbyte>.Empty;
                continue;
            }

            validData[i] = true;
            rows[i] = ToSbyteMemory(values[i]!);
        }

        return new Int8VectorFieldData(field.Name, rows) { ValidData = values.Any(v => v is null) ? validData : null };
    }

    private static ReadOnlyMemory<sbyte> ToSbyteMemory(object value)
        => value switch
        {
            ReadOnlyMemory<sbyte> memory => memory,
            sbyte[] array => array,
            IEnumerable<sbyte> enumerable => enumerable.ToArray(),
            IEnumerable<object> objects => objects.Select(o => Convert.ToSByte(o, CultureInfo.InvariantCulture)).ToArray(),
            _ => throw new ArgumentException($"Cannot convert '{value.GetType().Name}' to an int8 vector.")
        };

    private static ArrayFieldData<object?> BuildArray(FieldSchema field, List<object?> values)
    {
        DataType elementType = field.ElementDataType ?? throw new ArgumentException(
            $"Array field '{field.Name}' has no element data type.");

        var rows = new IReadOnlyList<object?>?[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] is null)
            {
                rows[i] = null;
                continue;
            }

            rows[i] = values[i] switch
            {
                IReadOnlyList<object?> list => list,
                IEnumerable enumerable => enumerable.Cast<object?>().ToList(),
                _ => throw new ArgumentException(
                    $"Cannot convert '{values[i]!.GetType().Name}' to an array for field '{field.Name}'.")
            };
        }

        // Use the internal ctor carrying the schema element type so ArrayFieldData<object?> encodes with the
        // declared element type instead of throwing ResolveElementType(typeof(object)) at serialization time.
        return new ArrayFieldData<object?>(field.Name, rows, elementType, field.IsDynamic)
        {
            ValidData = values.Any(v => v is null) ? values.Select(v => v is not null).ToArray() : null
        };
    }
}
