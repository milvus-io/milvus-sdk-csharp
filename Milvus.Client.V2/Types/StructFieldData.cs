using System.Globalization;

using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A <see cref="DataType.Struct" /> field's data: one list of struct values per row, where each struct value
/// is a dictionary mapping sub-field names to values. Mirrors the C++ SDK's <c>StructFieldData</c> (a
/// <c>vector&lt;vector&lt;nlohmann::json&gt;&gt;</c>) and Java's row-based struct insert data.
/// </summary>
public sealed class StructFieldData : FieldData<IReadOnlyList<IDictionary<string, object?>>?>
{
    private readonly IReadOnlyList<FieldSchema> _subFields;

    /// <summary>
    /// Creates a new struct field data instance.
    /// </summary>
    /// <param name="fieldName">The struct field's name.</param>
    /// <param name="data">One entry per row; each entry is the list of struct elements for that row, where each
    /// element is a dictionary mapping sub-field names to values.</param>
    /// <param name="subFields">The struct field's sub-field schema, used to encode each struct element.</param>
    public StructFieldData(
        string fieldName,
        IReadOnlyList<IReadOnlyList<IDictionary<string, object?>>?> data,
        IReadOnlyList<FieldSchema> subFields)
        : base(fieldName, data, DataType.Struct)
    {
        Verify.NotNull(subFields);
        if (subFields.Count == 0)
        {
            throw new ArgumentException("A struct field must have at least one sub-field.", nameof(subFields));
        }

        _subFields = subFields;
    }

    /// <inheritdoc />
    internal override FieldData SliceCore(int start, int count)
        => new StructFieldData(FieldName, Data.Skip(start).Take(count).ToList(), _subFields);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var field = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = Grpc.DataType.ArrayOfStruct,
            IsDynamic = IsDynamic,
            StructArrays = new Grpc.StructArrayField()
        };

        foreach (FieldSchema subField in _subFields)
        {
            field.StructArrays.Fields.Add(EncodeSubField(subField));
        }

        return field;
    }

    private Grpc.FieldData EncodeSubField(FieldSchema subField)
    {
        var result = new Grpc.FieldData { FieldName = subField.Name };

        // The server clears top-level valid_data for StructArrays and only honors per-sub-field valid_data,
        // so a null (or caller-flagged invalid) struct row must be emitted as an empty data slot plus a false
        // per-row flag here, mirroring the server's GenNullableStructArrayFieldData.
        var rowValid = new bool[RowCount];
        for (int row = 0; row < RowCount; row++)
        {
            rowValid[row] = IsRowValid(row) && Data[row] is not null;
        }

        if (IsVectorType(subField.DataType))
        {
            // Each row packs all its struct elements' vectors into one VectorField (dim = schema dim), appended
            // to VectorArray.data. Mirrors Java's genVectorArray / C++'s FillStructProtoFields.
            result.Type = Grpc.DataType.ArrayOfVector;
            var vectorArray = new Grpc.VectorArray
            {
                Dim = subField.Dimension ?? 0,
                ElementType = (Grpc.DataType)(int)subField.DataType
            };

            for (int row = 0; row < RowCount; row++)
            {
                vectorArray.Data.Add(EncodeRowVectors(subField, row));
            }

            result.Vectors = new Grpc.VectorField
            {
                Dim = subField.Dimension ?? 0,
                VectorArray = vectorArray
            };
        }
        else
        {
            // Each row packs all its struct elements' scalar values into one ScalarField, appended to
            // ArrayArray.data.
            result.Type = Grpc.DataType.Array;
            var arrayArray = new Grpc.ArrayArray { ElementType = (Grpc.DataType)(int)subField.DataType };

            for (int row = 0; row < RowCount; row++)
            {
                arrayArray.Data.Add(EncodeRowScalars(subField, row));
            }

            result.Scalars = new Grpc.ScalarField { ArrayData = arrayArray };
        }

        if (rowValid.Any(v => !v))
        {
            result.ValidData.AddRange(rowValid);
        }

        return result;
    }

    private Grpc.VectorField EncodeRowVectors(FieldSchema subField, int row)
    {
        var vectorField = new Grpc.VectorField { Dim = subField.Dimension ?? 0 };

        if (!IsRowValid(row))
        {
            return vectorField;
        }

        IReadOnlyList<IDictionary<string, object?>>? rowStructs = Data[row];
        if (rowStructs is null)
        {
            return vectorField;
        }

        switch (subField.DataType)
        {
            case DataType.FloatVector:
                var floatArray = new Grpc.FloatArray();
                int expectedDim = subField.Dimension ?? 0;
                foreach (IDictionary<string, object?> element in rowStructs)
                {
                    float[] vector = ToFloatVector(GetSubFieldValue(subField.Name, element, row), subField.Name);
                    if (expectedDim > 0 && vector.Length != expectedDim)
                    {
                        throw new ArgumentException(
                            $"Struct sub-field '{subField.Name}' row {row} has a vector of dimension {vector.Length}, " +
                            $"expected {expectedDim}.");
                    }

                    floatArray.Data.AddRange(vector);
                }

                vectorField.FloatVector = floatArray;
                break;
            case DataType.BinaryVector:
                var packedBytes = new List<byte>();
                int expectedBinaryDim = subField.Dimension ?? 0;
                foreach (IDictionary<string, object?> element in rowStructs)
                {
                    byte[] vectorBytes = ToByteVector(GetSubFieldValue(subField.Name, element, row), subField.Name);
                    if (expectedBinaryDim > 0 && vectorBytes.Length * 8 != expectedBinaryDim)
                    {
                        throw new ArgumentException(
                            $"Struct sub-field '{subField.Name}' row {row} has a binary vector of {vectorBytes.Length} " +
                            $"bytes ({vectorBytes.Length * 8} bits), expected dimension {expectedBinaryDim}.");
                    }

                    packedBytes.AddRange(vectorBytes);
                }

                vectorField.BinaryVector = ByteString.CopyFrom(packedBytes.ToArray());
                break;
            case DataType.Float16Vector:
                vectorField.Float16Vector = EncodeFloat16Vectors(rowStructs, subField, row);
                break;
            case DataType.BFloat16Vector:
                vectorField.Bfloat16Vector = EncodeFloat16Vectors(rowStructs, subField, row);
                break;
            case DataType.Int8Vector:
                var int8Bytes = new List<byte>();
                int expectedInt8Dim = subField.Dimension ?? 0;
                foreach (IDictionary<string, object?> element in rowStructs)
                {
                    sbyte[] sbytes = ToSbyteVector(GetSubFieldValue(subField.Name, element, row), subField.Name);
                    if (expectedInt8Dim > 0 && sbytes.Length != expectedInt8Dim)
                    {
                        throw new ArgumentException(
                            $"Struct sub-field '{subField.Name}' row {row} has an int8 vector of dimension " +
                            $"{sbytes.Length}, expected {expectedInt8Dim}.");
                    }

                    foreach (sbyte value in sbytes)
                    {
                        int8Bytes.Add(unchecked((byte)value));
                    }
                }

                vectorField.Int8Vector = ByteString.CopyFrom(int8Bytes.ToArray());
                break;
            case DataType.SparseFloatVector:
                foreach (IDictionary<string, object?> element in rowStructs)
                {
                    MilvusSparseVector<float> sparse = ToSparseVector(GetSubFieldValue(subField.Name, element, row), subField.Name);
                    vectorField.SparseFloatVector.Contents.Add(ByteString.CopyFrom(sparse.ToBytes()));
                }

                break;
            default:
                throw new NotSupportedException(
                    $"Struct sub-field vector type {subField.DataType} is not supported.");
        }

        return vectorField;
    }

    private Grpc.ScalarField EncodeRowScalars(FieldSchema subField, int row)
    {
        var scalar = new Grpc.ScalarField();

        if (!IsRowValid(row))
        {
            return scalar;
        }

        IReadOnlyList<IDictionary<string, object?>>? rowStructs = Data[row];
        if (rowStructs is null)
        {
            return scalar;
        }

        // Every struct element must provide each sub-field; a missing key is a caller error (mirroring the
        // C++ CheckAndSetFieldValue, which rejects non-nullable fields without a value).
        IList<object?> values = rowStructs
            .Select(e => GetSubFieldValue(subField.Name, e, row))
            .ToList();

        switch (subField.DataType)
        {
            case DataType.Bool:
                scalar.BoolData = new Grpc.BoolArray { Data = { values.Select(v => Convert.ToBoolean(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Int8:
            case DataType.Int16:
            case DataType.Int32:
                scalar.IntData = new Grpc.IntArray { Data = { values.Select(v => Convert.ToInt32(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Int64:
                scalar.LongData = new Grpc.LongArray { Data = { values.Select(v => Convert.ToInt64(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Float:
                scalar.FloatData = new Grpc.FloatArray { Data = { values.Select(v => Convert.ToSingle(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Double:
                scalar.DoubleData = new Grpc.DoubleArray { Data = { values.Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.VarChar:
            case DataType.String:
                scalar.StringData = new Grpc.StringArray { Data = { values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Json:
                var jsonArray = new Grpc.JSONArray();
                foreach (object? value in values)
                {
                    jsonArray.Data.Add(ByteString.CopyFromUtf8(System.Text.Json.JsonSerializer.Serialize(value)));
                }

                scalar.JsonData = jsonArray;
                break;
            default:
                throw new NotSupportedException(
                    $"Struct sub-field scalar type {subField.DataType} is not supported.");
        }

        return scalar;
    }

    // Returns a struct element's sub-field value, rejecting a *missing* key (a caller error) and an explicit
    // null (struct scalar sub-fields have no valid_data mechanism, so null would be silently coerced to
    // false/0/"" on the wire). Mirrors the C++ CheckAndSetFieldValue behavior for non-nullable struct
    // sub-fields.
    private static object? GetSubFieldValue(string name, IDictionary<string, object?> element, int row)
    {
        if (!element.TryGetValue(name, out object? value))
        {
            throw new ArgumentException(
                $"Struct element in row {row} is missing sub-field '{name}'; every struct element must " +
                $"provide a value for each sub-field.");
        }

        if (value is null)
        {
            throw new ArgumentException(
                $"Struct element in row {row} has a null value for sub-field '{name}'; struct sub-fields " +
                $"cannot be null.");
        }

        return value;
    }

    private static float[] ToFloatVector(object? value, string name)
    {
        if (value is null)
        {
            return Array.Empty<float>();
        }

        try
        {
            return value switch
            {
                ReadOnlyMemory<float> memory => memory.ToArray(),
                float[] array => array,
                IEnumerable<float> enumerable => enumerable.ToArray(),
                IEnumerable<object> objects => objects.Select(o => Convert.ToSingle(o, CultureInfo.InvariantCulture)).ToArray(),
                _ => throw new ArgumentException($"Cannot convert '{value.GetType().Name}' to a float vector for struct sub-field '{name}'.")
            };
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Cannot convert struct sub-field '{name}' value to a float vector: {ex.Message}");
        }
    }

    private static byte[] ToByteVector(object? value, string name)
        => value switch
        {
            null => Array.Empty<byte>(),
            ReadOnlyMemory<byte> memory => memory.ToArray(),
            byte[] array => array,
            IEnumerable<byte> enumerable => enumerable.ToArray(),
            _ => throw new ArgumentException($"Cannot convert '{value.GetType().Name}' to a binary vector for struct sub-field '{name}'.")
        };

    private static sbyte[] ToSbyteVector(object? value, string name)
        => value switch
        {
            null => Array.Empty<sbyte>(),
            ReadOnlyMemory<sbyte> memory => memory.ToArray(),
            sbyte[] array => array,
            IEnumerable<sbyte> enumerable => enumerable.ToArray(),
            IEnumerable<object> objects => objects.Select(o => Convert.ToSByte(o, CultureInfo.InvariantCulture)).ToArray(),
            _ => throw new ArgumentException($"Cannot convert '{value.GetType().Name}' to an int8 vector for struct sub-field '{name}'.")
        };

    private static ByteString EncodeFloat16Vectors(
        IReadOnlyList<IDictionary<string, object?>> rowStructs, FieldSchema subField, int row)
    {
        var bytes = new List<byte>();
        int expectedDim = subField.Dimension ?? 0;
        foreach (IDictionary<string, object?> element in rowStructs)
        {
            object? value = GetSubFieldValue(subField.Name, element, row);
            ReadOnlyMemory<ushort> half = value switch
            {
                null => ReadOnlyMemory<ushort>.Empty,
                ReadOnlyMemory<ushort> memory => memory,
                ushort[] array => array,
                IEnumerable<ushort> enumerable => enumerable.ToArray(),
                _ => throw new ArgumentException(
                    $"Cannot convert '{value.GetType().Name}' to a float16 vector for struct sub-field '{subField.Name}'.")
            };

            if (expectedDim > 0 && half.Length != expectedDim)
            {
                throw new ArgumentException(
                    $"Struct sub-field '{subField.Name}' row {row} has a half-precision vector of dimension " +
                    $"{half.Length}, expected {expectedDim}.");
            }

            foreach (ushort h in half.Span)
            {
                bytes.Add((byte)(h & 0xFF));
                bytes.Add((byte)(h >> 8));
            }
        }

        return ByteString.CopyFrom(bytes.ToArray());
    }

    private static MilvusSparseVector<float> ToSparseVector(object? value, string name)
    {
        if (value is MilvusSparseVector<float> sparse)
        {
            return sparse;
        }

        if (value is IDictionary<object, object?> dict)
        {
            var pairs = new List<KeyValuePair<int, float>>();
            foreach (KeyValuePair<object, object?> pair in dict)
            {
                pairs.Add(new KeyValuePair<int, float>(
                    Convert.ToInt32(pair.Key, CultureInfo.InvariantCulture),
                    Convert.ToSingle(pair.Value, CultureInfo.InvariantCulture)));
            }

            // MilvusSparseVector requires strictly ascending indices; dictionary enumeration order is not
            // guaranteed to be sorted, so sort by index before constructing (same fix as RowDataConverter).
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

        throw new ArgumentException($"Cannot convert '{value!.GetType().Name}' to a sparse vector for struct sub-field '{name}'.");
    }

    private static bool IsVectorType(DataType dataType)
        => dataType is DataType.FloatVector or DataType.Float16Vector or DataType.BFloat16Vector
            or DataType.BinaryVector or DataType.Int8Vector or DataType.SparseFloatVector;
}
