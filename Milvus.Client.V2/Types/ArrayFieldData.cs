using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Types;

/// <summary>
/// A field whose rows are arrays of a scalar element type (Milvus <see cref="DataType.Array" />).
/// </summary>
/// <typeparam name="TElement">The element type of the arrays.</typeparam>
public sealed class ArrayFieldData<TElement> : FieldData<IReadOnlyList<TElement>?>
{
    private static readonly DataType? _elementType = ResolveElementType(typeof(TElement));

    /// <summary>
    /// The data type of the array elements.
    /// </summary>
    public DataType ElementType => _elementType ?? throw new NotSupportedException(
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

        bool hasNullArrays = Data.Contains(null);
        foreach (IReadOnlyList<TElement>? row in Data)
        {
            if (row is null)
            {
                fieldData.ValidData.Add(false);
                continue;
            }

            if (hasNullArrays)
            {
                fieldData.ValidData.Add(true);
            }

            var scalar = new Grpc.ScalarField();

            switch (_elementType)
            {
                case DataType.Bool:
                    scalar.BoolData = new Grpc.BoolArray { Data = { row.Cast<bool>() } };
                    break;
                case DataType.Int8:
                    scalar.IntData = new Grpc.IntArray { Data = { row.Cast<sbyte>().Select(x => (int)x) } };
                    break;
                case DataType.Int16:
                    scalar.IntData = new Grpc.IntArray { Data = { row.Cast<short>().Select(x => (int)x) } };
                    break;
                case DataType.Int32:
                    scalar.IntData = new Grpc.IntArray { Data = { row.Cast<int>() } };
                    break;
                case DataType.Int64:
                    scalar.LongData = new Grpc.LongArray { Data = { row.Cast<long>() } };
                    break;
                case DataType.Float:
                    scalar.FloatData = new Grpc.FloatArray { Data = { row.Cast<float>() } };
                    break;
                case DataType.Double:
                    scalar.DoubleData = new Grpc.DoubleArray { Data = { row.Cast<double>() } };
                    break;
                case DataType.VarChar:
                case DataType.String:
                    scalar.StringData = new Grpc.StringArray { Data = { row.Cast<string>() } };
                    break;
                default:
                    throw new NotSupportedException($"Array element type '{_elementType}' is not supported");
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
