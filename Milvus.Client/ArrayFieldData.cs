namespace Milvus.Client;

/// <summary>
/// Array Field
/// </summary>
public sealed class ArrayFieldData<TElementData> : FieldData<IReadOnlyList<TElementData>>
{
    /// <summary>
    /// Construct an array field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <param name="isDynamic"></param>
    public ArrayFieldData(string fieldName, IReadOnlyList<IReadOnlyList<TElementData>?> data, bool isDynamic)
        : base(fieldName, data, MilvusDataType.Array, isDynamic)
    {
        ElementType = EnsureDataType<TElementData>();
    }

    /// <summary>
    /// Array element type
    /// </summary>
    public MilvusDataType ElementType { get; }

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        Check();

        Grpc.FieldData fieldData = new()
        {
            Type = Grpc.DataType.Array,
            IsDynamic = IsDynamic
        };

        if (FieldName is not null)
        {
            fieldData.FieldName = FieldName;
        }

        var arrayArray = new ArrayArray
        {
            ElementType = (DataType) ElementType,
        };

        fieldData.Scalars = new ScalarField
        {
            ArrayData = arrayArray
        };

        bool hasNullArrays = Data.Contains(null);
        foreach (var array in Data)
        {
            // Handle null arrays (array-level nullability) - only populate ValidData if there are nulls
            if (hasNullArrays)
            {
                if (array is null)
                {
                    fieldData.ValidData.Add(false);
                    continue;
                }

                fieldData.ValidData.Add(true);
            }

            switch (ElementType)
            {
                case MilvusDataType.Bool:
                    BoolArray boolData = new();
                    boolData.Data.AddRange((IEnumerable<bool>)array!);
                    arrayArray.Data.Add(new ScalarField { BoolData = boolData, });
                    break;

                case MilvusDataType.Int8:
                    IntArray int8Data = new();
                    int8Data.Data.AddRange(((IEnumerable<sbyte>)array!).Select(x => (int) x));
                    arrayArray.Data.Add(new ScalarField { IntData = int8Data, });
                    break;

                case MilvusDataType.Int16:
                    IntArray int16Data = new();
                    int16Data.Data.AddRange(((IEnumerable<short>)array!).Select(x => (int) x));
                    arrayArray.Data.Add(new ScalarField { IntData = int16Data, });
                    break;

                case MilvusDataType.Int32:
                    IntArray int32Data = new();
                    int32Data.Data.AddRange((IEnumerable<int>)array!);
                    arrayArray.Data.Add(new ScalarField { IntData = int32Data, });
                    break;

                case MilvusDataType.Int64:
                    LongArray int64Data = new();
                    int64Data.Data.AddRange((IEnumerable<long>)array!);
                    arrayArray.Data.Add(new ScalarField { LongData = int64Data, });
                    break;

                case MilvusDataType.Float:
                    FloatArray floatData = new();
                    floatData.Data.AddRange((IEnumerable<float>)array!);
                    arrayArray.Data.Add(new ScalarField { FloatData = floatData, });
                    break;

                case MilvusDataType.Double:
                    DoubleArray doubleData = new();
                    doubleData.Data.AddRange((IEnumerable<double>)array!);
                    arrayArray.Data.Add(new ScalarField { DoubleData = doubleData, });
                    break;

                case MilvusDataType.String:
                    StringArray stringData = new();
                    stringData.Data.AddRange((IEnumerable<string>)array!);
                    arrayArray.Data.Add(new ScalarField { StringData = stringData, });
                    break;

                case MilvusDataType.VarChar:
                    StringArray varcharData = new();
                    varcharData.Data.AddRange((IEnumerable<string>)array!);
                    arrayArray.Data.Add(new ScalarField { StringData = varcharData, });
                    break;

                case MilvusDataType.Json:
                    JSONArray jsonData = new();
                    jsonData.Data.AddRange(((IEnumerable<string>)array!).Select(ByteString.CopyFromUtf8));
                    arrayArray.Data.Add(new ScalarField { JsonData = jsonData, });
                    break;

                case MilvusDataType.None:
                    throw new MilvusException($"ElementType Error:{DataType}");

                default:
                    throw new MilvusException($"ElementType Error:{DataType}, not supported");
            }
        }

        return fieldData;
    }

    internal override object GetValueAsObject(int index)
        => ElementType switch
        {
            MilvusDataType.Bool => ((IReadOnlyList<IEnumerable<bool>>) Data)[index],
            MilvusDataType.Int8 => ((IReadOnlyList<IEnumerable<sbyte>>) Data)[index],
            MilvusDataType.Int16 => ((IReadOnlyList<IEnumerable<short>>) Data)[index],
            MilvusDataType.Int32 => ((IReadOnlyList<IEnumerable<int>>) Data)[index],
            MilvusDataType.Int64 => ((IReadOnlyList<IEnumerable<long>>) Data)[index],
            MilvusDataType.Float => ((IReadOnlyList<IEnumerable<float>>) Data)[index],
            MilvusDataType.Double => ((IReadOnlyList<IEnumerable<double>>) Data)[index],
            MilvusDataType.String => ((IReadOnlyList<IEnumerable<string>>) Data)[index],
            MilvusDataType.VarChar => ((IReadOnlyList<IEnumerable<string>>) Data)[index],

            MilvusDataType.None => throw new MilvusException($"DataType Error:{DataType}"),
            _ => throw new MilvusException($"DataType Error:{DataType}, not supported")
        };
}
