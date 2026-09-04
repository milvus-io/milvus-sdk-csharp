using System.Globalization;

using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A field whose rows are arrays of a scalar element type (Milvus <see cref="DataType.Array" />).
/// </summary>
/// <typeparam name="TElement">The element type of the arrays.</typeparam>
public sealed class ArrayFieldData<TElement> : FieldData<IReadOnlyList<TElement>?>
{
    private static readonly DataType? _elementType = ResolveElementType(typeof(TElement));
    private readonly DataType? _elementTypeOverride;

    /// <summary>
    /// The data type of the array elements.
    /// </summary>
    public DataType ElementType => _elementTypeOverride ?? _elementType ?? throw new NotSupportedException(
        $"Array element type '{typeof(TElement)}' is not supported");

    /// <summary>
    /// Creates a new array field data instance.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="data">The array rows; a <c>null</c> element represents a null array.</param>
    /// <param name="isDynamic">Whether this is a dynamic field.</param>
    public ArrayFieldData(string fieldName, IReadOnlyList<IReadOnlyList<TElement>?> data, bool isDynamic = false)
        : base(fieldName, data, DataType.Array, isDynamic)
    {
        Verify.NotNull(data);
    }

    // Row-based inserts produce object-typed rows but know the schema element type; carry it explicitly so
    // ArrayFieldData<object?> still encodes with the declared element type.
    internal ArrayFieldData(
        string fieldName, IReadOnlyList<IReadOnlyList<TElement>?> data, DataType elementType, bool isDynamic = false)
        : base(fieldName, data, DataType.Array, isDynamic)
    {
        Verify.NotNull(data);
        _elementTypeOverride = elementType;
    }

    /// <inheritdoc />
    internal override FieldData SliceCore(int start, int count)
        => _elementTypeOverride is { } overrideType
            ? new ArrayFieldData<TElement>(FieldName, Data.Skip(start).Take(count).ToList(), overrideType, IsDynamic)
            : new ArrayFieldData<TElement>(FieldName, Data.Skip(start).Take(count).ToList(), IsDynamic);

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        var fieldData = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = Grpc.DataType.Array,
            IsDynamic = IsDynamic
        };

        var arrayArray = new Grpc.ArrayArray
        {
            ElementType = (Grpc.DataType)(int)ElementType
        };

        bool hasNullArrays = Data.Contains(null) || ValidData?.Any(v => !v) == true;
        for (int i = 0; i < Data.Count; i++)
        {
            IReadOnlyList<TElement>? row = Data[i];
            // Honor ValidData (a caller-flagged invalid row) in addition to literal null arrays.
            if (!IsRowValid(i) || row is null)
            {
                fieldData.ValidData.Add(false);
                continue;
            }

            if (hasNullArrays)
            {
                fieldData.ValidData.Add(true);
            }

            var scalar = new Grpc.ScalarField();

            // Convert (not Cast<T>) so boxed narrow-numeric elements (e.g. int rows supplied for an Int64
            // element type) encode via the declared element type instead of throwing InvalidCastException.
            switch (ElementType)
            {
                case DataType.Bool:
                    scalar.BoolData = new Grpc.BoolArray { Data = { row.Select(x => Convert.ToBoolean(x, CultureInfo.InvariantCulture)) } };
                    break;
                case DataType.Int8:
                    scalar.IntData = new Grpc.IntArray { Data = { row.Select(x => (int)Convert.ToSByte(x, CultureInfo.InvariantCulture)) } };
                    break;
                case DataType.Int16:
                    scalar.IntData = new Grpc.IntArray { Data = { row.Select(x => (int)Convert.ToInt16(x, CultureInfo.InvariantCulture)) } };
                    break;
                case DataType.Int32:
                    scalar.IntData = new Grpc.IntArray { Data = { row.Select(x => Convert.ToInt32(x, CultureInfo.InvariantCulture)) } };
                    break;
                case DataType.Int64:
                    scalar.LongData = new Grpc.LongArray { Data = { row.Select(x => Convert.ToInt64(x, CultureInfo.InvariantCulture)) } };
                    break;
                case DataType.Float:
                    scalar.FloatData = new Grpc.FloatArray { Data = { row.Select(x => Convert.ToSingle(x, CultureInfo.InvariantCulture)) } };
                    break;
                case DataType.Double:
                    scalar.DoubleData = new Grpc.DoubleArray { Data = { row.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture)) } };
                    break;
                case DataType.VarChar:
                case DataType.String:
                    // Google.Protobuf string fields cannot hold null; reject a null element with a clear
                    // error here instead of an ArgumentNullException deep inside request serialization.
                    if (row.Any(x => x is null))
                    {
                        throw new ArgumentException(
                            $"Array field '{FieldName}' contains a null string element, which the wire does not support.");
                    }

                    scalar.StringData = new Grpc.StringArray { Data = { row.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture) ?? "") } };
                    break;
                default:
                    throw new NotSupportedException($"Array element type '{ElementType}' is not supported");
            }

            arrayArray.Data.Add(scalar);
        }

        fieldData.Scalars = new Grpc.ScalarField { ArrayData = arrayArray };
        return fieldData;
    }

    private static DataType? ResolveElementType(Type elementType)
        => elementType == typeof(bool) ? DataType.Bool
            : elementType == typeof(sbyte) ? DataType.Int8
            : elementType == typeof(short) ? DataType.Int16
            : elementType == typeof(int) ? DataType.Int32
            : elementType == typeof(long) ? DataType.Int64
            : elementType == typeof(float) ? DataType.Float
            : elementType == typeof(double) ? DataType.Double
            : elementType == typeof(string) ? DataType.VarChar
            : (DataType?)null;
}
