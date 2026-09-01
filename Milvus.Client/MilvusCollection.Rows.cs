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
    /// dynamic field rather than rejected.
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
    private static FieldData BuildDynamicColumn(string name, object?[] values)
    {
        object? sample = values.FirstOrDefault(v => v is not null);

        return sample switch
        {
            null => FieldData.CreateVarChar(name, values.Select(_ => (string?)null).ToList(), isDynamic: true),
            bool => FieldData.Create(name, Cast<bool>(name, values), isDynamic: true),
            sbyte or short or int or long => FieldData.Create(name, values.Select(ToInt64).ToList(), isDynamic: true),
            float or double => FieldData.Create(name, values.Select(ToDouble).ToList(), isDynamic: true),
            string => FieldData.CreateVarChar(name, values.Select(v => (string?)v).ToList(), isDynamic: true),
            _ => throw new MilvusException(
                $"Dynamic field '{name}' has unsupported value type '{sample.GetType()}'. Supported types are " +
                "bool, integers, floating point numbers and strings.")
        };
    }

    private static FieldData BuildColumn(FieldSchema field, object?[] values)
        => field.DataType switch
        {
            MilvusDataType.Bool => FieldData.Create(field.Name, Cast<bool>(field.Name, values)),
            MilvusDataType.Int8 => FieldData.Create(field.Name, values.Select(v => (sbyte)ToInt64(v)).ToList()),
            MilvusDataType.Int16 => FieldData.Create(field.Name, values.Select(v => (short)ToInt64(v)).ToList()),
            MilvusDataType.Int32 => FieldData.Create(field.Name, values.Select(v => (int)ToInt64(v)).ToList()),
            MilvusDataType.Int64 => FieldData.Create(field.Name, values.Select(ToInt64).ToList()),
            MilvusDataType.Float => FieldData.Create(field.Name, values.Select(v => (float)ToDouble(v)).ToList()),
            MilvusDataType.Double => FieldData.Create(field.Name, values.Select(ToDouble).ToList()),

            MilvusDataType.String or MilvusDataType.VarChar
                => FieldData.CreateVarChar(field.Name, values.Select(ToNullableString).ToList()),
            MilvusDataType.Json
                => FieldData.CreateJson(field.Name, values.Select(v => ToNullableString(v) ?? "{}").ToList()),
            MilvusDataType.Geometry
                => FieldData.CreateGeometry(field.Name, values.Select(ToNullableString).ToList()),
            MilvusDataType.Timestamptz
                => FieldData.CreateTimestamptz(field.Name, values.Select(ToTimestamptzString).ToList()),

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

    private static long ToInt64(object? value)
        => value switch
        {
            null => throw new MilvusException("Null is not valid for a non-nullable integer field."),
            sbyte v => v,
            byte v => v,
            short v => v,
            ushort v => v,
            int v => v,
            uint v => v,
            long v => v,
            _ => throw new MilvusException($"Cannot convert '{value.GetType()}' to an integer field value.")
        };

    private static double ToDouble(object? value)
        => value switch
        {
            null => throw new MilvusException("Null is not valid for a non-nullable floating point field."),
            float v => v,
            double v => v,
            sbyte or byte or short or ushort or int or uint or long => ToInt64(value),
            _ => throw new MilvusException($"Cannot convert '{value.GetType()}' to a floating point field value.")
        };

    private static string? ToNullableString(object? value)
        => value switch
        {
            null => null,
            string s => s,
            _ => throw new MilvusException($"Cannot convert '{value.GetType()}' to a string field value.")
        };

    /// <summary>
    /// Timestamptz travels as ISO 8601 text, so accept the natural .NET date types as well as a
    /// pre-formatted string.
    /// </summary>
    private static string? ToTimestamptzString(object? value)
        => value switch
        {
            null => null,
            string s => s,
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            _ => throw new MilvusException($"Cannot convert '{value.GetType()}' to a Timestamptz field value.")
        };
}
