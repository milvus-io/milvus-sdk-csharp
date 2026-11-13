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
    internal override FieldData SelectRowsCore(IReadOnlyList<int> indices)
        => new StructFieldData(FieldName, indices.Select(i => Data[i]).ToList(), _subFields);

    /// <inheritdoc />
    internal override FieldData ConcatCore(FieldData other)
        => new StructFieldData(FieldName, Data.Concat(((StructFieldData)other).Data).ToList(), _subFields);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        // Reject struct elements carrying keys that are not declared sub-fields (mirroring pymilvus's rejection
        // of unexpected fields on insert): a typo'd key would otherwise be silently ignored on the wire.
        var subFieldNames = new HashSet<string>(_subFields.Select(f => f.Name), StringComparer.Ordinal);
        for (int row = 0; row < RowCount; row++)
        {
            if (Data[row] is not { } rowStructs)
            {
                continue;
            }

            foreach (IDictionary<string, object?> element in rowStructs)
            {
                foreach (string key in element.Keys)
                {
                    if (!subFieldNames.Contains(key))
                    {
                        throw new ArgumentException(
                            $"Struct element in row {row} has an unexpected field '{key}'; expected one of the " +
                            "declared sub-fields.", nameof(Data));
                    }
                }
            }
        }

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

        // The 2.6 server's checkAndFlattenStructFieldData requires compacted sub-field payloads: only the
        // valid rows are packed (len(payload) == validCount), with per-sub-field valid_data marking which
        // logical rows are present. Null struct rows must therefore be omitted from the payload entirely and
        // flagged false here, mirroring the server's GenNullableStructArrayFieldData.
        var rowValid = new bool[RowCount];
        for (int row = 0; row < RowCount; row++)
        {
            rowValid[row] = IsRowValid(row) && Data[row] is not null;
        }

        if (IsVectorType(subField.DataType))
        {
            // Each row packs all its struct elements' vectors into one VectorField (dim = schema dim), appended
            // to VectorArray.data. Mirrors Java's genVectorArray / C++'s FillStructProtoFields.
            long dimValue = subField.Dimension ?? 0;
            if (subField.DataType == DataType.SparseFloatVector)
            {
                // Sparse fields carry no schema dimension; report maxDim = (last index + 1) like the top-level
                // sparse encoder so the read path (which keys off Dim > 0) can decode the contents.
                for (int row = 0; row < RowCount; row++)
                {
                    if (!rowValid[row] || Data[row] is not { } rowStructs)
                    {
                        continue;
                    }

                    foreach (IDictionary<string, object?> element in rowStructs)
                    {
                        if (TryGetSubFieldValue(subField, element, row, out object? subValue))
                        {
                            SparseVector<float> sparse = ToSparseVector(subValue!, subField.Name);
                            int lastIndex = sparse.Indices.Length == 0 ? -1 : sparse.Indices.Span[sparse.Indices.Length - 1];
                            dimValue = Math.Max(dimValue, (long)lastIndex + 1);
                        }
                    }
                }
            }

            result.Type = Grpc.DataType.ArrayOfVector;
            var vectorArray = new Grpc.VectorArray
            {
                Dim = dimValue,
                ElementType = (Grpc.DataType)(int)subField.DataType
            };

            for (int row = 0; row < RowCount; row++)
            {
                if (rowValid[row])
                {
                    vectorArray.Data.Add(EncodeRowVectors(subField, row));
                }
            }

            result.Vectors = new Grpc.VectorField
            {
                Dim = dimValue,
                VectorArray = vectorArray
            };
        }
        else if (subField.DataType == DataType.Array)
        {
            // Nested Array sub-field: the outer ArrayArray element type is Array (the sub-field's own type)
            // and each per-row cell is itself an ArrayArray of leaf elements. Mirrors pymilvus's recursive
            // array encoding and the Java genStructSubFieldData nested-Array branch.
            result.Type = Grpc.DataType.Array;
            var arrayArray = new Grpc.ArrayArray { ElementType = Grpc.DataType.Array };

            for (int row = 0; row < RowCount; row++)
            {
                if (rowValid[row])
                {
                    arrayArray.Data.Add(EncodeRowNestedArray(subField, row));
                }
            }

            result.Scalars = new Grpc.ScalarField { ArrayData = arrayArray };
        }
        else
        {
            // Each row packs all its struct elements' scalar values into one ScalarField, appended to
            // ArrayArray.data.
            result.Type = Grpc.DataType.Array;
            var arrayArray = new Grpc.ArrayArray { ElementType = (Grpc.DataType)(int)subField.DataType };

            for (int row = 0; row < RowCount; row++)
            {
                if (rowValid[row])
                {
                    arrayArray.Data.Add(EncodeRowScalars(subField, row));
                }
            }

            result.Scalars = new Grpc.ScalarField { ArrayData = arrayArray };
        }

        if (rowValid.Any(v => !v))
        {
            result.ValidData.AddRange(rowValid);

            // schema.proto requires the row-validity mask to be dual-written to both the FieldData.valid_data
            // and the sub-field payload's ScalarField/VectorField valid_data (a reader may consult either,
            // preferring the FieldData location); mirror pymilvus's sync_row_validity. The payload-level mask
            // is the row mask here, distinct from the per-row-cell element-level valid_data.
            if (result.Scalars is not null)
            {
                result.Scalars.ValidData.AddRange(rowValid);
            }

            if (result.Vectors is not null)
            {
                result.Vectors.ValidData.AddRange(rowValid);
            }
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

        // Element-null vectors are compacted with per-element valid_data only when the sub-field is
        // element-nullable; otherwise a null element is rejected (mirroring the C++ CheckAndSetStructVectorValue).
        bool elementNullable = subField.Nullable;

        switch (subField.DataType)
        {
            case DataType.FloatVector:
                var floatArray = new Grpc.FloatArray();
                int expectedDim = subField.Dimension ?? 0;
                foreach (IDictionary<string, object?> element in rowStructs)
                {
                    if (!TryGetSubFieldValue(subField, element, row, out object? subValue))
                    {
                        if (!elementNullable)
                        {
                            throw RejectNullElement(subField, row);
                        }

                        vectorField.ValidData.Add(false);
                        continue;
                    }

                    if (elementNullable)
                    {
                        vectorField.ValidData.Add(true);
                    }

                    float[] vector = ToFloatVector(subValue, subField.Name);
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
                    if (!TryGetSubFieldValue(subField, element, row, out object? subValue))
                    {
                        if (!elementNullable)
                        {
                            throw RejectNullElement(subField, row);
                        }

                        vectorField.ValidData.Add(false);
                        continue;
                    }

                    if (elementNullable)
                    {
                        vectorField.ValidData.Add(true);
                    }

                    byte[] vectorBytes = ToByteVector(subValue, subField.Name);
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
                vectorField.Float16Vector = EncodeFloat16Vectors(vectorField, rowStructs, subField, row, elementNullable);
                break;
            case DataType.BFloat16Vector:
                vectorField.Bfloat16Vector = EncodeFloat16Vectors(vectorField, rowStructs, subField, row, elementNullable);
                break;
            case DataType.Int8Vector:
                var int8Bytes = new List<byte>();
                int expectedInt8Dim = subField.Dimension ?? 0;
                foreach (IDictionary<string, object?> element in rowStructs)
                {
                    if (!TryGetSubFieldValue(subField, element, row, out object? subValue))
                    {
                        if (!elementNullable)
                        {
                            throw RejectNullElement(subField, row);
                        }

                        vectorField.ValidData.Add(false);
                        continue;
                    }

                    if (elementNullable)
                    {
                        vectorField.ValidData.Add(true);
                    }

                    sbyte[] sbytes = ToSbyteVector(subValue, subField.Name);
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
                // SparseFloatVector is a oneof message member whose getter returns null until assigned;
                // initialize it explicitly before appending (mirrors the top-level sparse encoder).
                vectorField.SparseFloatVector = new Grpc.SparseFloatArray();
                foreach (IDictionary<string, object?> element in rowStructs)
                {
                    if (!TryGetSubFieldValue(subField, element, row, out object? subValue))
                    {
                        if (!elementNullable)
                        {
                            throw RejectNullElement(subField, row);
                        }

                        vectorField.ValidData.Add(false);
                        continue;
                    }

                    if (elementNullable)
                    {
                        vectorField.ValidData.Add(true);
                    }

                    SparseVector<float> sparse = ToSparseVector(subValue, subField.Name);
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

        // A nullable sub-field accepts a missing key or an explicit null as a null element (compacted out and
        // flagged through the cell's per-element valid_data); a non-element-nullable sub-field rejects both.
        // Mirrors the Java processStructFieldValues / C++ CheckAndSetStructScalarValue behavior.
        bool elementNullable = subField.Nullable;
        var validity = new List<bool>();
        var present = new List<object?>();
        foreach (IDictionary<string, object?> element in rowStructs)
        {
            if (!TryGetSubFieldValue(subField, element, row, out object? value))
            {
                validity.Add(false);
                continue;
            }

            validity.Add(true);
            present.Add(value);
        }

        switch (subField.DataType)
        {
            case DataType.Bool:
                scalar.BoolData = new Grpc.BoolArray { Data = { present.Select(v => Convert.ToBoolean(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Int8:
            case DataType.Int16:
            case DataType.Int32:
                scalar.IntData = new Grpc.IntArray { Data = { present.Select(v => Convert.ToInt32(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Int64:
                scalar.LongData = new Grpc.LongArray { Data = { present.Select(v => Convert.ToInt64(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Float:
                scalar.FloatData = new Grpc.FloatArray { Data = { present.Select(v => Convert.ToSingle(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Double:
                scalar.DoubleData = new Grpc.DoubleArray { Data = { present.Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.VarChar:
            case DataType.String:
                scalar.StringData = new Grpc.StringArray { Data = { present.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)) } };
                break;
            case DataType.Json:
                var jsonArray = new Grpc.JSONArray();
                foreach (object? value in present)
                {
                    jsonArray.Data.Add(ByteString.CopyFromUtf8(System.Text.Json.JsonSerializer.Serialize(value)));
                }

                scalar.JsonData = jsonArray;
                break;
            default:
                throw new NotSupportedException(
                    $"Struct sub-field scalar type {subField.DataType} is not supported.");
        }

        // Element-level validity is written only when the sub-field is element-nullable, matching pymilvus's
        // compacted encoding (validity is empty when not nullable, so even an all-present row carries none).
        if (elementNullable)
        {
            scalar.ValidData.AddRange(validity);
        }

        return scalar;
    }

    // Encodes one struct row's nested-Array sub-field values into a per-row ScalarField cell. The cell's own
    // valid_data marks which inner arrays are null (sub-field Nullable); its ArrayData holds one leaf
    // ScalarField per non-null inner array, and each leaf's valid_data marks null values inside the inner
    // array (sub-field ElementNullable). Mirrors Java genStructNestedArrayRow / C++ CheckAndSetStructScalarValue.
    private Grpc.ScalarField EncodeRowNestedArray(FieldSchema subField, int row)
    {
        var rowCell = new Grpc.ScalarField
        {
            ArrayData = new Grpc.ArrayArray { ElementType = (Grpc.DataType)(int)subField.ElementDataType! }
        };

        if (!IsRowValid(row))
        {
            return rowCell;
        }

        IReadOnlyList<IDictionary<string, object?>>? rowStructs = Data[row];
        if (rowStructs is null)
        {
            return rowCell;
        }

        bool arrayNullable = subField.Nullable;
        bool leafNullable = subField.ElementNullable;

        foreach (IDictionary<string, object?> element in rowStructs)
        {
            if (!TryGetSubFieldValue(subField, element, row, out object? value))
            {
                // a null inner array
                if (!arrayNullable)
                {
                    throw new ArgumentException(
                        $"Struct sub-field '{subField.Name}' row {row} does not allow null inner arrays " +
                        "(set Nullable on the Array sub-field).");
                }

                rowCell.ValidData.Add(false);
                continue;
            }

            if (arrayNullable)
            {
                rowCell.ValidData.Add(true);
            }

            if (value is not System.Collections.IEnumerable enumerable || value is string)
            {
                throw new ArgumentException(
                    $"Struct sub-field '{subField.Name}' row {row} expects an array value, got " +
                    $"'{value!.GetType().Name}'.");
            }

            var leaf = new Grpc.ScalarField();
            foreach (object? leafValue in enumerable)
            {
                if (leafValue is null)
                {
                    if (!leafNullable)
                    {
                        throw new ArgumentException(
                            $"Struct sub-field '{subField.Name}' row {row} does not allow null elements " +
                            "(set ElementNullable on the Array sub-field).");
                    }

                    leaf.ValidData.Add(false);
                    continue;
                }

                if (leafNullable)
                {
                    leaf.ValidData.Add(true);
                }

                AppendScalarValue(leaf, subField.ElementDataType!.Value, leafValue, subField.Name, row);
            }

            // An empty inner array must still carry its typed array (DataCase pointing at the real slot), not
            // an unset ScalarField: the server treats an unset DataCase as a null inner array, so a valid
            // empty array would otherwise be rejected as an undeclared null value (matching pymilvus, which
            // sets the typed slot even for an empty array).
            if (leaf.DataCase == Grpc.ScalarField.DataOneofCase.None)
            {
                EnsureLeafArray(leaf, subField.ElementDataType!.Value);
            }

            rowCell.ArrayData.Data.Add(leaf);
        }

        return rowCell;
    }

    // Appends one leaf value to the nested-array leaf ScalarField, dispatching by the leaf element type.
    private static void AppendScalarValue(Grpc.ScalarField leaf, DataType elementType, object value, string name, int row)
    {
        switch (elementType)
        {
            case DataType.Bool:
                leaf.BoolData ??= new Grpc.BoolArray();
                leaf.BoolData.Data.Add(Convert.ToBoolean(value, CultureInfo.InvariantCulture));
                break;
            case DataType.Int8:
            case DataType.Int16:
            case DataType.Int32:
                leaf.IntData ??= new Grpc.IntArray();
                leaf.IntData.Data.Add(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                break;
            case DataType.Int64:
                leaf.LongData ??= new Grpc.LongArray();
                leaf.LongData.Data.Add(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case DataType.Float:
                leaf.FloatData ??= new Grpc.FloatArray();
                leaf.FloatData.Data.Add(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                break;
            case DataType.Double:
                leaf.DoubleData ??= new Grpc.DoubleArray();
                leaf.DoubleData.Data.Add(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            case DataType.VarChar:
            case DataType.String:
                leaf.StringData ??= new Grpc.StringArray();
                leaf.StringData.Data.Add(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            default:
                throw new ArgumentException(
                    $"Struct sub-field '{name}' row {row} has an unsupported array element type {elementType}.");
        }
    }

    private static ArgumentException RejectNullElement(FieldSchema subField, int row)
        => new($"Struct element in row {row} has a null value for sub-field '{subField.Name}'; " +
               $"sub-field '{subField.Name}' is not element-nullable (set Nullable on the sub-field).");

    // Creates the typed array slot of an empty nested-array leaf so the ScalarField oneof points at the real
    // slot (an empty array) rather than an unset DataCase (which the server reads as a null inner array).
    private static void EnsureLeafArray(Grpc.ScalarField leaf, DataType elementType)
    {
        switch (elementType)
        {
            case DataType.Bool:
                leaf.BoolData ??= new Grpc.BoolArray();
                break;
            case DataType.Int8:
            case DataType.Int16:
            case DataType.Int32:
                leaf.IntData ??= new Grpc.IntArray();
                break;
            case DataType.Int64:
                leaf.LongData ??= new Grpc.LongArray();
                break;
            case DataType.Float:
                leaf.FloatData ??= new Grpc.FloatArray();
                break;
            case DataType.Double:
                leaf.DoubleData ??= new Grpc.DoubleArray();
                break;
            case DataType.VarChar:
            case DataType.String:
                leaf.StringData ??= new Grpc.StringArray();
                break;
            default:
                break;
        }
    }

    // Returns a struct element's sub-field value. For an element-nullable sub-field (or a nested-Array
    // sub-field whose inner arrays may be null) a missing key or an explicit null yields null, encoded as a
    // null element; for a non-element-nullable sub-field both are rejected.
    private static bool TryGetSubFieldValue(
        FieldSchema subField, IDictionary<string, object?> element, int row, out object? value)
    {
        if (!element.TryGetValue(subField.Name, out value))
        {
            if (subField.Nullable)
            {
                value = null;
                return false;
            }

            throw new ArgumentException(
                $"Struct element in row {row} is missing sub-field '{subField.Name}'; every struct element " +
                $"must provide a value for each sub-field.");
        }

        if (value is null && !subField.Nullable)
        {
            throw new ArgumentException(
                $"Struct element in row {row} has a null value for sub-field '{subField.Name}'; struct " +
                $"sub-field '{subField.Name}' is not element-nullable.");
        }

        return value is not null;
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
        Grpc.VectorField vectorField,
        IReadOnlyList<IDictionary<string, object?>> rowStructs, FieldSchema subField, int row, bool elementNullable)
    {
        var bytes = new List<byte>();
        int expectedDim = subField.Dimension ?? 0;
        foreach (IDictionary<string, object?> element in rowStructs)
        {
            if (!TryGetSubFieldValue(subField, element, row, out object? value))
            {
                if (!elementNullable)
                {
                    throw RejectNullElement(subField, row);
                }

                vectorField.ValidData.Add(false);
                continue;
            }

            if (elementNullable)
            {
                vectorField.ValidData.Add(true);
            }

            ReadOnlyMemory<ushort> half = value switch
            {
                ReadOnlyMemory<ushort> memory => memory,
                ushort[] array => array,
                IEnumerable<ushort> enumerable => enumerable.ToArray(),
                _ => throw new ArgumentException(
                    $"Cannot convert '{value!.GetType().Name}' to a float16 vector for struct sub-field '{subField.Name}'.")
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

    private static SparseVector<float> ToSparseVector(object? value, string name)
    {
        if (value is SparseVector<float> sparse)
        {
            return sparse;
        }

        // Mirrors RowDataConverter.ToSparseVector so a struct sparse sub-field accepts the same dictionary
        // shapes as the top-level sparse column (generic interface parameters are invariant, so
        // Dictionary<int,float> etc. do not implement IDictionary<object, object?>).
        IEnumerable<KeyValuePair<object, object?>>? dictPairs = value switch
        {
            IDictionary<object, object?> d => d,
            IDictionary<long, float> d => CastDictionary(d),
            IDictionary<int, float> d => CastDictionary(d),
            IDictionary<string, float> d => CastStringDictionary(d),
            IDictionary<string, double> d => CastStringDictionary(d),
            IDictionary<string, int> d => CastStringDictionary(d),
            IDictionary<string, long> d => CastStringDictionary(d),
            IDictionary<string, object> d => CastStringDictionary(d),
            IDictionary<int, double> d => CastDictionary(d),
            IDictionary<int, long> d => CastDictionary(d),
            IDictionary<long, double> d => CastDictionary(d),
            _ => null
        };

        if (dictPairs is not null)
        {
            var pairs = new List<KeyValuePair<int, float>>();
            foreach (KeyValuePair<object, object?> pair in dictPairs)
            {
                pairs.Add(new KeyValuePair<int, float>(
                    Convert.ToInt32(pair.Key, CultureInfo.InvariantCulture),
                    Convert.ToSingle(pair.Value, CultureInfo.InvariantCulture)));
            }

            // SparseVector requires strictly ascending indices; dictionary enumeration order is not
            // guaranteed to be sorted, so sort by index before constructing (same fix as RowDataConverter).
            pairs.Sort((a, b) => a.Key.CompareTo(b.Key));
            var indices = new int[pairs.Count];
            var sparseValues = new float[pairs.Count];
            for (int i = 0; i < pairs.Count; i++)
            {
                indices[i] = pairs[i].Key;
                sparseValues[i] = pairs[i].Value;
            }

            return new SparseVector<float>(indices, sparseValues);
        }

        throw new ArgumentException($"Cannot convert '{value!.GetType().Name}' to a sparse vector for struct sub-field '{name}'.");
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

    private static bool IsVectorType(DataType dataType)
        => dataType is DataType.FloatVector or DataType.Float16Vector or DataType.BFloat16Vector
            or DataType.BinaryVector or DataType.Int8Vector or DataType.SparseFloatVector;
}
