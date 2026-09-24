using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dml;

/// <summary>
/// Represents a request to insert rows into a collection.
/// </summary>
/// <remarks>
/// <see cref="ColumnsData" /> (columnar) and <see cref="RowsData" /> (row-based) are mutually exclusive. When
/// <see cref="RowsData" /> is used, the client describes the collection (cache-served) and converts the rows to
/// columns, consuming <see cref="RowsData" /> in the process. An auto-id primary key may be omitted on insert.
/// </remarks>
public sealed class InsertReq
{
    /// <summary>
    /// An optional database name to operate on. When empty, the client's currently selected database is used,
    /// matching the Java/C++ SDKs' request-level database override.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The name of the collection to insert into.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The column-based data to insert; each field contains one value per row. Mirrors the C++ SDK's
    /// <c>ColumnsData</c>. Mutually exclusive with <see cref="RowsData" />.
    /// </summary>
    public IReadOnlyList<FieldData> ColumnsData { get; set; } = Array.Empty<FieldData>();

    /// <summary>
    /// Optional row-based data to insert: one dictionary per row mapping field names to values. Mirrors the
    /// C++ SDK's <c>RowsData</c>. Mutually exclusive with <see cref="ColumnsData" />. Values are encoded
    /// according to the collection schema (see the Java SDK's <c>InsertReq.data</c>): numeric scalars accept
    /// the boxed CLR type or a convertible string; vectors accept <see cref="ReadOnlyMemory{T}" />, arrays,
    /// or enumerables; sparse vectors accept index/value dictionaries; arrays accept any
    /// <see cref="System.Collections.IEnumerable" />. With dynamic fields enabled, unknown keys are collected
    /// into the collection's <c>$meta</c> JSON field.
    /// </summary>
    public IReadOnlyList<IDictionary<string, object?>> RowsData { get; set; } = Array.Empty<IDictionary<string, object?>>();

    /// <summary>
    /// An optional partition to insert into.
    /// </summary>
    public string? PartitionName { get; set; }

    /// <summary>
    /// The timestamp of the collection schema used to encode the data, sent as <c>schema_timestamp</c>.
    /// When non-zero, the server can detect a stale schema and return <c>SchemaMismatch</c>, letting the
    /// client refresh its cache and retry. Mirrors the Java SDK's <c>setSchemaTimestamp</c>.
    /// </summary>
    internal ulong SchemaTimestamp { get; set; }

    internal Grpc.InsertRequest ToGrpcInsertRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(ColumnsData);
        foreach (FieldData field in ColumnsData)
        {
            if (field is null)
            {
                throw new ArgumentException("ColumnsData must not contain null entries.", nameof(ColumnsData));
            }
        }

        if (ColumnsData[0].RowCount <= 0)
        {
            throw new ArgumentException("ColumnsData must contain at least one row.", nameof(ColumnsData));
        }

        var request = new Grpc.InsertRequest
        {
            CollectionName = CollectionName,
            PartitionName = PartitionName ?? "",
            NumRows = (uint)ColumnsData[0].RowCount
        };

        if (SchemaTimestamp != 0)
        {
            request.SchemaTimestamp = SchemaTimestamp;
        }

        // Dynamic fields are aggregated into a single JSON metadata field (mirroring V1).
        Dictionary<string, object?>?[]? dynamicFieldsData = null;
        int rowCount = (int)request.NumRows;

        foreach (FieldData field in ColumnsData)
        {
            if (field.RowCount != rowCount)
            {
                throw new ArgumentException(
                    $"Field '{field.FieldName}' contains {field.RowCount} rows, but {rowCount} were expected; " +
                    "all fields in ColumnsData must contain the same number of rows.");
            }

            if (field.IsDynamic)
            {
                // A nameless dynamic FieldData (FieldName == "") is the pre-aggregated whole-row $meta column
                // produced by RowDataConverter.ConvertRows; pass it through as-is instead of re-aggregating
                // (which would double-encode it under an empty key). A named dynamic field is a user-supplied
                // per-row key and goes through the aggregation below.
                if (field.FieldName.Length == 0)
                {
                    request.FieldsData.Add(field.ToGrpcFieldData());
                    continue;
                }

                dynamicFieldsData ??= new Dictionary<string, object?>[rowCount];
                for (int rowNum = 0; rowNum < rowCount; rowNum++)
                {
                    Dictionary<string, object?> rowDynamicData =
                        dynamicFieldsData[rowNum] ?? (dynamicFieldsData[rowNum] = new Dictionary<string, object?>());

                    object? value = field.GetValueAsObject(rowNum);

                    // Dynamic fields are stored per row, so each row's $meta object carries only the keys that
                    // row actually has. A null means this row has no value for the key, and omitting it is not
                    // the same as writing an explicit JSON null -- the latter would make the key exist with a
                    // null value (matching the V1 SDK).
                    if (value is not null)
                    {
                        rowDynamicData[field.FieldName] = value;
                    }
                }
            }
            else
            {
                request.FieldsData.Add(field.ToGrpcFieldData());
            }
        }

        if (dynamicFieldsData is not null)
        {
            var encodedJson = new string[rowCount];
            for (int rowNum = 0; rowNum < rowCount; rowNum++)
            {
                // A row with no dynamic keys must serialize as an empty JSON object, not the literal JSON null
                // that JsonSerializer.Serialize(null) emits (the server expects an object in the $meta column).
                Dictionary<string, object?>? rowDynamic = dynamicFieldsData[rowNum] ?? new Dictionary<string, object?>();
                encodedJson[rowNum] = System.Text.Json.JsonSerializer.Serialize(rowDynamic);
            }

            request.FieldsData.Add(FieldData.CreateDynamicJson(encodedJson).ToGrpcFieldData());
        }

        request.DbName = DatabaseName ?? "";
        return request;
    }
}
