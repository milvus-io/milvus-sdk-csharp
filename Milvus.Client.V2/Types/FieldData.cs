using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// Base type for a single field's data within an insert/upsert row set.
/// </summary>
public abstract class FieldData
{
    /// <summary>
    /// Creates a scalar field data for the given values.
    /// </summary>
    public static FieldData<TData> Create<TData>(string fieldName, IReadOnlyList<TData> data, bool isDynamic = false)
        => new(fieldName, data, isDynamic);

    /// <summary>
    /// Creates a <see cref="DataType.VarChar" /> field data.
    /// </summary>
    public static FieldData<string> CreateVarChar(string fieldName, IReadOnlyList<string> data, bool isDynamic = false)
        => new(fieldName, data, isDynamic);

    /// <summary>
    /// Creates a <see cref="DataType.FloatVector" /> field data.
    /// </summary>
    public static FloatVectorFieldData CreateFloatVector(string fieldName, IReadOnlyList<ReadOnlyMemory<float>> data)
        => new(fieldName, data);

    /// <summary>
    /// Creates a <see cref="DataType.Float16Vector" /> field data (each row is a list of FP16 bit patterns).
    /// </summary>
    public static Float16VectorFieldData CreateFloat16Vector(
        string fieldName, IReadOnlyList<ReadOnlyMemory<ushort>> data)
        => new(fieldName, data);

    /// <summary>
    /// Creates a <see cref="DataType.BFloat16Vector" /> field data (each row is a list of BFloat16 bit patterns).
    /// </summary>
    public static BFloat16VectorFieldData CreateBFloat16Vector(
        string fieldName, IReadOnlyList<ReadOnlyMemory<ushort>> data)
        => new(fieldName, data);

    /// <summary>
    /// Creates a <see cref="DataType.BinaryVector" /> field data.
    /// </summary>
    public static BinaryVectorFieldData CreateBinaryVectors(string fieldName, IReadOnlyList<ReadOnlyMemory<byte>> data)
        => new(fieldName, data);

    /// <summary>
    /// Creates a <see cref="DataType.SparseFloatVector" /> field data.
    /// </summary>
    public static SparseFloatVectorFieldData CreateSparseFloatVector(
        string fieldName, IReadOnlyList<MilvusSparseVector<float>> data)
        => new(fieldName, data);

    /// <summary>
    /// Creates a <see cref="DataType.Json" /> field data (each row is a JSON string).
    /// </summary>
    public static FieldData<string> CreateJson(string fieldName, IReadOnlyList<string> json, bool isDynamic = false)
        => new(fieldName, json, DataType.Json, isDynamic);

    /// <summary>
    /// Creates the aggregated JSON metadata field that packs all dynamic fields into one row per entry,
    /// using the server-side <c>$meta</c> column. The field carries no schema-level name.
    /// </summary>
    internal static FieldData<string> CreateDynamicJson(IReadOnlyList<string> json)
        => new(json, DataType.Json, isDynamic: true);

    /// <summary>
    /// The name of the field this data belongs to.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The Milvus data type of the field.
    /// </summary>
    public DataType DataType { get; }

    /// <summary>
    /// Whether this is a dynamic (undefined) field.
    /// </summary>
    public bool IsDynamic { get; }

    /// <summary>
    /// Optional per-row validity flags for nullable fields. When set, rows whose flag is <c>false</c> are
    /// treated as <c>null</c>: their values are not packed on the wire, and the flags are propagated via the
    /// proto <c>valid_data</c>. When <c>null</c> (the default), all rows are considered valid.
    /// </summary>
    public IReadOnlyList<bool>? ValidData { get; set; }

    // Assigns ValidData from a decoded wire mask. Implemented on the generic subclass (all concrete field
    // types derive from it); used by query/search decoding to round-trip nullable columns through re-insert.
    internal abstract void SetValidData(IReadOnlyList<bool> validData);

    // True when ValidData is unset or the row's flag is true.
    internal bool IsRowValid(int index)
        => ValidData is null || ValidData[index];

    // Appends the per-row validity flags to the proto field when ValidData is set. Called by vector field
    // encoders; scalar encoders that have their own null-packing (string/json) must not call this.
    internal void AddValidData(Grpc.FieldData field)
    {
        if (ValidData is not null)
        {
            field.ValidData.AddRange(ValidData);
        }
    }

    /// <summary>
    /// The number of rows in this field's data.
    /// </summary>
    public abstract int RowCount { get; }

    private protected FieldData(string fieldName, DataType dataType, bool isDynamic = false)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        FieldName = fieldName;
        DataType = dataType;
        IsDynamic = isDynamic;
    }

    // Used only internally for dynamic fields, which get serialized to JSON. The aggregated dynamic
    // JSON field has no schema-level name (it maps to the server-side "$meta" column).
    private protected FieldData(DataType dataType, bool isDynamic)
    {
        FieldName = "";
        DataType = dataType;
        IsDynamic = isDynamic;
    }

    internal abstract Grpc.FieldData ToGrpcFieldData();

    /// <summary>
    /// Gets the raw value of a row (used to aggregate dynamic fields into a JSON metadata field).
    /// </summary>
    internal abstract object? GetValueAsObject(int index);

    /// <summary>
    /// Returns a copy of this column restricted to the rows in <c>[start, start+count)</c>, preserving the
    /// field name, data type and dynamic flag. Used to slice a per-query search result out of the flat
    /// N*topk response and to cap an over-delivered iterator page.
    /// </summary>
    internal abstract FieldData Slice(int start, int count);
}

/// <summary>
/// A field whose rows are values of type <typeparamref name="TData" /> (scalars or vector rows).
/// </summary>
public class FieldData<TData> : FieldData
{
    /// <summary>
    /// The field data values, one element per row.
    /// </summary>
    public IReadOnlyList<TData> Data { get; }

    internal override void SetValidData(IReadOnlyList<bool> validData)
        => ValidData = validData;

    /// <summary>
    /// Creates a new field data instance.
    /// </summary>
    public FieldData(string fieldName, IReadOnlyList<TData> data, bool isDynamic = false)
        : base(fieldName, EnsureDataType<TData>(), isDynamic)
    {
        Verify.NotNull(data);
        Data = data;
    }

    internal FieldData(string fieldName, IReadOnlyList<TData> data, DataType dataType, bool isDynamic = false)
        : base(fieldName, dataType, isDynamic)
    {
        Verify.NotNull(data);
        Data = data;
    }

    internal FieldData(IReadOnlyList<TData> data, DataType dataType, bool isDynamic)
        : base(dataType, isDynamic)
    {
        Verify.NotNull(data);
        Data = data;
    }

    /// <inheritdoc />
    public override int RowCount => Data.Count;

    /// <inheritdoc />
    internal override object? GetValueAsObject(int index)
        => Data[index];

    /// <inheritdoc />
    internal override FieldData Slice(int start, int count)
    {
        FieldData sliced = SliceCore(start, count);
        if (ValidData is not null)
        {
            sliced.ValidData = ValidData.Skip(start).Take(count).ToList();
        }

        return sliced;
    }

    // Subclasses that carry a distinct concrete type (vector fields) override this to preserve their type
    // through a slice; the default reconstructs the base FieldData<TData>.
    internal virtual FieldData SliceCore(int start, int count)
    {
        IReadOnlyList<TData> sliced = Data.Skip(start).Take(count).ToList();
        // Dynamic $meta columns produced on the read path carry an empty field name; preserve that shape
        // through the name-less constructor rather than tripping the blank-name guard.
        return string.IsNullOrEmpty(FieldName)
            ? new FieldData<TData>(sliced, DataType, IsDynamic)
            : new FieldData<TData>(FieldName, sliced, DataType, IsDynamic);
    }

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var field = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)(int)DataType,
            IsDynamic = IsDynamic
        };

        Type? nullableType = Nullable.GetUnderlyingType(typeof(TData));
        bool isNullable = nullableType is not null;

        if (DataType == DataType.Json)
        {
            var jsonData = new Grpc.JSONArray();
            IReadOnlyList<string?> jsonStrings = (IReadOnlyList<string?>)(object)Data;
            bool anyNull = jsonStrings.Any(v => v is null) || ValidData?.Any(v => !v) == true;
            for (int i = 0; i < jsonStrings.Count; i++)
            {
                // Honor ValidData (a caller-flagged invalid row) in addition to literal nulls.
                if (!IsRowValid(i) || jsonStrings[i] is null)
                {
                    field.ValidData.Add(false);
                }
                else
                {
                    field.ValidData.Add(true);
                    jsonData.Data.Add(ByteString.CopyFromUtf8(jsonStrings[i]));
                }
            }

            if (!anyNull)
            {
                field.ValidData.Clear();
            }

            field.Scalars = new Grpc.ScalarField { JsonData = jsonData };
        }
        else if (nullableType == typeof(sbyte) || nullableType == typeof(short) || nullableType == typeof(int))
        {
            field.Scalars = new Grpc.ScalarField { IntData = ToIntArray() };
        }
        else if (nullableType == typeof(long))
        {
            field.Scalars = new Grpc.ScalarField { LongData = ToLongArray() };
        }
        else if (nullableType == typeof(bool))
        {
            field.Scalars = new Grpc.ScalarField { BoolData = ToBoolArray() };
        }
        else if (nullableType == typeof(float))
        {
            field.Scalars = new Grpc.ScalarField { FloatData = ToFloatArray() };
        }
        else if (nullableType == typeof(double))
        {
            field.Scalars = new Grpc.ScalarField { DoubleData = ToDoubleArray() };
        }
        else if (typeof(TData) == typeof(string))
        {
            IReadOnlyList<string?> stringValues = (IReadOnlyList<string?>)(object)Data;
            bool anyNull = stringValues.Any(v => v is null) || ValidData?.Any(v => !v) == true;

            if (DataType == DataType.Geometry)
            {
                // Geometry rows travel as WKT strings in the dedicated geometry_wkt_data slot (the
                // StringData slot is rejected by the proxy for geometry fields), mirroring the Java SDK.
                var geometryData = new Grpc.GeometryWktArray();
                for (int i = 0; i < stringValues.Count; i++)
                {
                    if (!IsRowValid(i) || stringValues[i] is null)
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        geometryData.Data.Add(stringValues[i]);
                    }
                }

                if (!anyNull)
                {
                    field.ValidData.Clear();
                }

                field.Scalars = new Grpc.ScalarField { GeometryWktData = geometryData };
            }
            else
            {
                var stringData = new Grpc.StringArray();
                for (int i = 0; i < stringValues.Count; i++)
                {
                    // Honor ValidData (a caller-flagged invalid row) in addition to literal nulls.
                    if (!IsRowValid(i) || stringValues[i] is null)
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        stringData.Data.Add(stringValues[i]);
                    }
                }

                if (!anyNull)
                {
                    field.ValidData.Clear();
                }

                field.Scalars = new Grpc.ScalarField { StringData = stringData };
            }
        }
        else if (typeof(TData) == typeof(sbyte) || typeof(TData) == typeof(short) || typeof(TData) == typeof(int))
        {
            field.Scalars = new Grpc.ScalarField { IntData = ToIntArray() };
        }
        else if (typeof(TData) == typeof(long))
        {
            field.Scalars = new Grpc.ScalarField { LongData = ToLongArray() };
        }
        else if (typeof(TData) == typeof(bool))
        {
            field.Scalars = new Grpc.ScalarField { BoolData = ToBoolArray() };
        }
        else if (typeof(TData) == typeof(float))
        {
            field.Scalars = new Grpc.ScalarField { FloatData = ToFloatArray() };
        }
        else if (typeof(TData) == typeof(double))
        {
            field.Scalars = new Grpc.ScalarField { DoubleData = ToDoubleArray() };
        }
        else
        {
            throw new NotSupportedException($"Unsupported scalar data type '{typeof(TData)}' for field '{FieldName}'.");
        }

        return field;

        Grpc.IntArray ToIntArray()
        {
            var intData = new Grpc.IntArray();
            if (isNullable)
            {
                for (int i = 0; i < Data.Count; i++)
                {
                    if (!IsRowValid(i) || Data[i] is null)
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        intData.Data.Add(Convert.ToInt32(Data[i], System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            else if (ValidData is not null)
            {
                // Honor ValidData even on a non-nullable column: rows flagged false are not packed and emit a
                // valid_data=false flag, matching the nullable path and the documented ValidData contract.
                for (int i = 0; i < Data.Count; i++)
                {
                    if (!IsRowValid(i))
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        intData.Data.Add(Convert.ToInt32(Data[i], System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            else
            {
                intData.Data.AddRange(Data.Select(x => Convert.ToInt32(x, System.Globalization.CultureInfo.InvariantCulture)));
            }

            return intData;
        }

        Grpc.LongArray ToLongArray()
        {
            var longData = new Grpc.LongArray();
            if (isNullable)
            {
                IReadOnlyList<long?> longs = (IReadOnlyList<long?>)(object)Data;
                for (int i = 0; i < longs.Count; i++)
                {
                    if (!IsRowValid(i) || longs[i] is null)
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        longData.Data.Add(longs[i]!.Value);
                    }
                }
            }
            else if (ValidData is not null)
            {
                // Honor ValidData even on a non-nullable column: rows flagged false are not packed and emit a
                // valid_data=false flag, matching the nullable path and the documented ValidData contract.
                for (int i = 0; i < Data.Count; i++)
                {
                    if (!IsRowValid(i))
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        longData.Data.Add(Convert.ToInt64(Data[i], System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            else
            {
                longData.Data.AddRange(Data.Cast<long>());
            }

            return longData;
        }

        Grpc.BoolArray ToBoolArray()
        {
            var boolData = new Grpc.BoolArray();
            if (isNullable)
            {
                IReadOnlyList<bool?> bools = (IReadOnlyList<bool?>)(object)Data;
                for (int i = 0; i < bools.Count; i++)
                {
                    if (!IsRowValid(i) || bools[i] is null)
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        boolData.Data.Add(bools[i]!.Value);
                    }
                }
            }
            else if (ValidData is not null)
            {
                // Honor ValidData even on a non-nullable column: rows flagged false are not packed and emit a
                // valid_data=false flag, matching the nullable path and the documented ValidData contract.
                for (int i = 0; i < Data.Count; i++)
                {
                    if (!IsRowValid(i))
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        boolData.Data.Add(Convert.ToBoolean(Data[i], System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            else
            {
                boolData.Data.AddRange(Data.Cast<bool>());
            }

            return boolData;
        }

        Grpc.FloatArray ToFloatArray()
        {
            var floatData = new Grpc.FloatArray();
            if (isNullable)
            {
                IReadOnlyList<float?> floats = (IReadOnlyList<float?>)(object)Data;
                for (int i = 0; i < floats.Count; i++)
                {
                    if (!IsRowValid(i) || floats[i] is null)
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        floatData.Data.Add(floats[i]!.Value);
                    }
                }
            }
            else if (ValidData is not null)
            {
                // Honor ValidData even on a non-nullable column: rows flagged false are not packed and emit a
                // valid_data=false flag, matching the nullable path and the documented ValidData contract.
                for (int i = 0; i < Data.Count; i++)
                {
                    if (!IsRowValid(i))
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        floatData.Data.Add(Convert.ToSingle(Data[i], System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            else
            {
                floatData.Data.AddRange(Data.Cast<float>());
            }

            return floatData;
        }

        Grpc.DoubleArray ToDoubleArray()
        {
            var doubleData = new Grpc.DoubleArray();
            if (isNullable)
            {
                IReadOnlyList<double?> doubles = (IReadOnlyList<double?>)(object)Data;
                for (int i = 0; i < doubles.Count; i++)
                {
                    if (!IsRowValid(i) || doubles[i] is null)
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        doubleData.Data.Add(doubles[i]!.Value);
                    }
                }
            }
            else if (ValidData is not null)
            {
                // Honor ValidData even on a non-nullable column: rows flagged false are not packed and emit a
                // valid_data=false flag, matching the nullable path and the documented ValidData contract.
                for (int i = 0; i < Data.Count; i++)
                {
                    if (!IsRowValid(i))
                    {
                        field.ValidData.Add(false);
                    }
                    else
                    {
                        field.ValidData.Add(true);
                        doubleData.Data.Add(Convert.ToDouble(Data[i], System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            else
            {
                doubleData.Data.AddRange(Data.Cast<double>());
            }

            return doubleData;
        }
    }

    internal static DataType EnsureDataType<T>()
        => typeof(T) switch
        {
            _ when typeof(T) == typeof(bool) || Nullable.GetUnderlyingType(typeof(T)) == typeof(bool) => DataType.Bool,
            _ when typeof(T) == typeof(sbyte) || Nullable.GetUnderlyingType(typeof(T)) == typeof(sbyte) => DataType.Int8,
            _ when typeof(T) == typeof(short) || Nullable.GetUnderlyingType(typeof(T)) == typeof(short) => DataType.Int16,
            _ when typeof(T) == typeof(int) || Nullable.GetUnderlyingType(typeof(T)) == typeof(int) => DataType.Int32,
            _ when typeof(T) == typeof(long) || Nullable.GetUnderlyingType(typeof(T)) == typeof(long) => DataType.Int64,
            _ when typeof(T) == typeof(float) || Nullable.GetUnderlyingType(typeof(T)) == typeof(float) => DataType.Float,
            _ when typeof(T) == typeof(double) || Nullable.GetUnderlyingType(typeof(T)) == typeof(double) => DataType.Double,
            _ when typeof(T) == typeof(string) => DataType.VarChar,
            _ => throw new ArgumentException($"Unsupported generic data type '{typeof(T)}'", nameof(T))
        };
}
