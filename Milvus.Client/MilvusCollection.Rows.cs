using System.Globalization;

namespace Milvus.Client;

public partial class MilvusCollection
{
    /// <summary>
    /// Inserts rows of data into a collection, given one dictionary per row.
    /// </summary>
    /// <param name="rows">
    /// The rows to insert. Each dictionary maps a field name to that row's value. Every row must
    /// contain the same fields, except that a field may be omitted or <c>null</c> when the schema
    /// marks it nullable, and auto-id primary keys must be omitted.
    /// </param>
    /// <param name="partitionName">An optional name of a partition to insert into.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// Milvus itself is column-oriented — the wire protocol only carries columns — so this method
    /// fetches the collection schema and pivots the rows into columns before sending them. It is a
    /// convenience over <see cref="InsertAsync(IReadOnlyList{FieldData}, string, CancellationToken)" />,
    /// not a distinct server operation, and it costs one extra <c>DescribeCollection</c> call.
    /// </para>
    /// <para>
    /// When the schema enables dynamic fields, any key not matching a declared field is sent as a
    /// dynamic field rather than rejected. Dynamic fields are per-row optional: a key present in one
    /// row and absent from another is sent as null for the rows that lack it.
    /// </para>
    /// </remarks>
    public async Task<MutationResult> InsertAsync(
        IReadOnlyList<IDictionary<string, object?>> rows,
        string? partitionName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(rows);

        if (rows.Count == 0)
        {
            throw new ArgumentException("At least one row must be provided.", nameof(rows));
        }

        MilvusCollectionDescription description = await DescribeAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<FieldData> columns = BuildColumns(rows, description.Schema, includeAutoId: false);

        return await InsertAsync(columns, partitionName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Upserts rows of data into a collection, given one dictionary per row.
    /// </summary>
    /// <param name="rows">The rows to upsert, in the same shape accepted by the row-based insert.</param>
    /// <param name="partitionName">An optional name of a partition to upsert into.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// Like the row-based insert, this pivots the rows into columns client-side and costs one extra
    /// <c>DescribeCollection</c> call.
    /// </para>
    /// <para>
    /// Unlike insert, an upsert has to identify the row it is replacing, so the primary key must be
    /// present in every row — including when the key is declared <c>autoId</c>, where insert would
    /// omit it. Milvus deletes the old row and inserts the new one, so a collection with an auto-id
    /// key assigns a fresh key rather than preserving the supplied one.
    /// </para>
    /// </remarks>
    public async Task<MutationResult> UpsertAsync(
        IReadOnlyList<IDictionary<string, object?>> rows,
        string? partitionName = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(rows);

        if (rows.Count == 0)
        {
            throw new ArgumentException("At least one row must be provided.", nameof(rows));
        }

        MilvusCollectionDescription description = await DescribeAsync(cancellationToken).ConfigureAwait(false);

        // includeAutoId: an upsert must carry the primary key even when it is auto-generated,
        // otherwise Milvus cannot tell which row is being replaced.
        IReadOnlyList<FieldData> columns = BuildColumns(rows, description.Schema, includeAutoId: true);

        return await UpsertAsync(columns, partitionName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Pivots row dictionaries into the column-oriented <see cref="FieldData" /> the wire protocol
    /// expects, using the collection schema to decide each column's type.
    /// </summary>
    private static List<FieldData> BuildColumns(
        IReadOnlyList<IDictionary<string, object?>> rows, CollectionSchema schema, bool includeAutoId)
    {
        List<FieldData> columns = new();
        HashSet<string> declaredFields = new(StringComparer.Ordinal);

        foreach (FieldSchema field in schema.Fields)
        {
            declaredFields.Add(field.Name);

            // Function outputs (e.g. a BM25 sparse vector) are always computed server-side. Auto-id
            // primary keys are generated on insert, but an upsert must still carry the key so the
            // server knows which row is being replaced.
            if (field.IsFunctionOutput || (field.AutoId && !includeAutoId))
            {
                continue;
            }

            object?[] values = new object?[rows.Count];
            bool anyPresent = false;

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].TryGetValue(field.Name, out object? value))
                {
                    values[i] = value;
                    anyPresent = true;
                }
            }

            // A nullable field absent from every row is simply not sent.
            if (!anyPresent && field.Nullable)
            {
                continue;
            }

            columns.Add(BuildColumn(field, values));
        }

        if (schema.EnableDynamicFields)
        {
            AddDynamicColumns(rows, declaredFields, columns);
        }
        else
        {
            foreach (IDictionary<string, object?> row in rows)
            {
                foreach (string key in row.Keys)
                {
                    if (!declaredFields.Contains(key))
                    {
                        throw new MilvusException(
                            $"Field '{key}' is not part of the collection schema, and the collection does not " +
                            "enable dynamic fields.");
                    }
                }
            }
        }

        return columns;
    }

    private static void AddDynamicColumns(
        IReadOnlyList<IDictionary<string, object?>> rows, HashSet<string> declaredFields, List<FieldData> columns)
    {
        // Preserve first-seen order so the generated columns are stable across calls.
        List<string> dynamicNames = new();
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (IDictionary<string, object?> row in rows)
        {
            foreach (string key in row.Keys)
            {
                if (!declaredFields.Contains(key) && seen.Add(key))
                {
                    dynamicNames.Add(key);
                }
            }
        }

        foreach (string name in dynamicNames)
        {
            object?[] values = new object?[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].TryGetValue(name, out values[i]);
            }

            columns.Add(BuildDynamicColumn(name, values));
        }
    }

    /// <summary>
    /// Builds a dynamic column. The element type is inferred from the first non-null value, since a
    /// dynamic field has no schema to consult.
    /// </summary>
    /// <remarks>
    /// Dynamic columns are always built nullable. Milvus stores dynamic fields per row, so each row
    /// may carry a different set of extra keys, and a row that lacks this one has to send null rather
    /// than fail the whole batch.
    /// </remarks>
    private static FieldData BuildDynamicColumn(string name, object?[] values)
    {
        object? sample = values.FirstOrDefault(v => v is not null);

        return sample switch
        {
            null => FieldData.CreateVarChar(name, new string?[values.Length], isDynamic: true),

            bool => ScalarColumn<bool>(name, values, ToBoolean, nullable: true, isDynamic: true),

            // The accepted set matches ToInt64 and ToDouble below, so inference and conversion cannot
            // disagree about which CLR types are usable.
            sbyte or byte or short or ushort or int or uint or long
                => ScalarColumn<long>(name, values, ToInt64, nullable: true, isDynamic: true),
            float or double
                => ScalarColumn<double>(name, values, ToDouble, nullable: true, isDynamic: true),

            string => FieldData.CreateVarChar(
                name, TextColumn(name, values, ToText, nullable: true), isDynamic: true),

            _ => throw new MilvusException(
                $"Dynamic field '{name}' has unsupported value type '{sample.GetType()}'. Supported types are " +
                "bool, integers, floating point numbers and strings.")
        };
    }

    private static FieldData BuildColumn(FieldSchema field, object?[] values)
        => field.DataType switch
        {
            MilvusDataType.Bool => ScalarColumn<bool>(field.Name, values, ToBoolean, field.Nullable),
            MilvusDataType.Int8 => ScalarColumn<sbyte>(field.Name, values, ToInt8, field.Nullable),
            MilvusDataType.Int16 => ScalarColumn<short>(field.Name, values, ToInt16, field.Nullable),
            MilvusDataType.Int32 => ScalarColumn<int>(field.Name, values, ToInt32, field.Nullable),
            MilvusDataType.Int64 => ScalarColumn<long>(field.Name, values, ToInt64, field.Nullable),
            MilvusDataType.Float => ScalarColumn<float>(field.Name, values, ToFloat, field.Nullable),
            MilvusDataType.Double => ScalarColumn<double>(field.Name, values, ToDouble, field.Nullable),

            MilvusDataType.String or MilvusDataType.VarChar
                => FieldData.CreateVarChar(field.Name, TextColumn(field.Name, values, ToText, field.Nullable)),
            MilvusDataType.Geometry
                => FieldData.CreateGeometry(field.Name, TextColumn(field.Name, values, ToText, field.Nullable)),
            MilvusDataType.Timestamptz
                => FieldData.CreateTimestamptz(
                    field.Name, TextColumn(field.Name, values, ToTimestamptzText, field.Nullable)),
            MilvusDataType.Json
                => FieldData.CreateJson(field.Name, JsonColumn(field.Name, values)),

            MilvusDataType.FloatVector
                => FieldData.CreateFloatVector(field.Name, Cast<ReadOnlyMemory<float>>(field.Name, values)),
            MilvusDataType.BinaryVector
                => FieldData.CreateBinaryVectors(field.Name, Cast<ReadOnlyMemory<byte>>(field.Name, values)),
#if NET8_0_OR_GREATER
            MilvusDataType.Float16Vector
                => FieldData.CreateFloat16Vector(field.Name, Cast<ReadOnlyMemory<Half>>(field.Name, values)),
#endif
            MilvusDataType.BFloat16Vector
                => FieldData.CreateBFloat16Vector(field.Name, Cast<ReadOnlyMemory<BFloat16>>(field.Name, values)),
            MilvusDataType.Int8Vector
                => FieldData.CreateInt8Vector(field.Name, Cast<ReadOnlyMemory<sbyte>>(field.Name, values)),
            MilvusDataType.SparseFloatVector
                => FieldData.CreateSparseFloatVector(
                    field.Name, Cast<MilvusSparseVector<float>>(field.Name, values)),

            _ => throw new MilvusException(
                $"Row-based insert does not support field '{field.Name}' of type {field.DataType}. Use the " +
                "column-based InsertAsync overload for this collection.")
        };

    private delegate T RowConverter<out T>(object value, string fieldName, int row);

    /// <summary>
    /// Projects one column of raw row values into typed <see cref="FieldData" />.
    /// </summary>
    /// <remarks>
    /// When the column accepts nulls the data is built as <c>List&lt;T?&gt;</c>, which is what makes
    /// <see cref="FieldData{TData}" /> emit the <c>valid_data</c> mask the server needs to distinguish
    /// a null from a zero. When it does not, a null is a caller mistake and is reported against the
    /// specific field and row rather than being coerced.
    /// </remarks>
    private static FieldData ScalarColumn<T>(
        string fieldName, object?[] values, RowConverter<T> convert, bool nullable, bool isDynamic = false)
        where T : struct
    {
        if (nullable)
        {
            List<T?> data = new(values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                data.Add(values[i] is null ? null : convert(values[i]!, fieldName, i));
            }

            return FieldData.Create(fieldName, data, isDynamic);
        }

        List<T> required = new(values.Length);

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] is null)
            {
                throw NullNotAllowed(fieldName, i);
            }

            required.Add(convert(values[i]!, fieldName, i));
        }

        return FieldData.Create(fieldName, required, isDynamic);
    }

    /// <summary>
    /// Projects a column whose values travel as text. The varchar, geometry and timestamptz wire paths
    /// all carry nulls themselves, so this only has to decide whether a null is allowed.
    /// </summary>
    private static List<string?> TextColumn(
        string fieldName, object?[] values, RowConverter<string> convert, bool nullable)
    {
        List<string?> data = new(values.Length);

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] is null)
            {
                data.Add(nullable ? null : throw NullNotAllowed(fieldName, i));
            }
            else
            {
                data.Add(convert(values[i]!, fieldName, i));
            }
        }

        return data;
    }

    private static List<string> JsonColumn(string fieldName, object?[] values)
    {
        List<string> data = new(values.Length);

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] is null)
            {
                // Unlike varchar, the JSON wire path has no valid_data branch, so a null cannot be
                // represented at all. Reporting it beats silently writing an empty object.
                throw new MilvusException(
                    $"Field '{fieldName}' is JSON and row {i} is null, which the wire protocol cannot " +
                    "represent. Pass the string \"{}\" if an empty object is what you mean.");
            }

            data.Add(ToText(values[i]!, fieldName, i));
        }

        return data;
    }

    private static T[] Cast<T>(string fieldName, object?[] values)
    {
        T[] result = new T[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] is T typed)
            {
                result[i] = typed;
            }
            else
            {
                throw new MilvusException(
                    $"Field '{fieldName}' expects values of type {typeof(T)}, but row {i} has " +
                    $"{(values[i] is null ? "null" : values[i]!.GetType().ToString())}.");
            }
        }

        return result;
    }

    private static bool ToBoolean(object value, string fieldName, int row)
        => value is bool converted ? converted : throw TypeMismatch(fieldName, row, value, "a boolean");

    private static long ToInt64(object value, string fieldName, int row)
        => value switch
        {
            sbyte v => v,
            byte v => v,
            short v => v,
            ushort v => v,
            int v => v,
            uint v => v,
            long v => v,
            _ => throw TypeMismatch(fieldName, row, value, "an integer")
        };

    // The narrower integer fields are range-checked rather than cast: an unchecked cast turns 300 for
    // an Int8 field into 44, writing a value the caller never supplied.
    private static sbyte ToInt8(object value, string fieldName, int row)
        => (sbyte)InRange(ToInt64(value, fieldName, row), sbyte.MinValue, sbyte.MaxValue, "Int8", fieldName, row);

    private static short ToInt16(object value, string fieldName, int row)
        => (short)InRange(ToInt64(value, fieldName, row), short.MinValue, short.MaxValue, "Int16", fieldName, row);

    private static int ToInt32(object value, string fieldName, int row)
        => (int)InRange(ToInt64(value, fieldName, row), int.MinValue, int.MaxValue, "Int32", fieldName, row);

    private static long InRange(long value, long min, long max, string typeName, string fieldName, int row)
        => value >= min && value <= max
            ? value
            : throw new MilvusException(
                $"Field '{fieldName}' is {typeName}, but row {row} has value {value}, which is outside the " +
                $"representable range {min} to {max}.");

    private static double ToDouble(object value, string fieldName, int row)
        => value switch
        {
            float v => v,
            double v => v,
            sbyte or byte or short or ushort or int or uint or long => ToInt64(value, fieldName, row),
            _ => throw TypeMismatch(fieldName, row, value, "a floating point number")
        };

    private static float ToFloat(object value, string fieldName, int row)
    {
        double wide = ToDouble(value, fieldName, row);
        float narrow = (float)wide;

        // Same problem as the integer casts, in a different disguise: a finite double outside float's
        // range narrows to infinity instead of failing.
        if (float.IsInfinity(narrow) && !double.IsInfinity(wide))
        {
            throw new MilvusException(
                $"Field '{fieldName}' is Float, but row {row} has value " +
                $"{wide.ToString("R", CultureInfo.InvariantCulture)}, which is outside the range of a " +
                "32-bit float.");
        }

        return narrow;
    }

    private static string ToText(object value, string fieldName, int row)
        => value is string converted ? converted : throw TypeMismatch(fieldName, row, value, "a string");

    /// <summary>
    /// Timestamptz travels as ISO 8601 text, so accept the natural .NET date types as well as a
    /// pre-formatted string.
    /// </summary>
    private static string ToTimestamptzText(object value, string fieldName, int row)
        => value switch
        {
            string s => s,
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            _ => throw TypeMismatch(fieldName, row, value, "a string, DateTime or DateTimeOffset")
        };

    private static MilvusException TypeMismatch(string fieldName, int row, object value, string expected)
        => new($"Field '{fieldName}' expects {expected}, but row {row} has {value.GetType()}.");

    private static MilvusException NullNotAllowed(string fieldName, int row)
        => new($"Field '{fieldName}' is not nullable, but row {row} has no value for it.");
}
