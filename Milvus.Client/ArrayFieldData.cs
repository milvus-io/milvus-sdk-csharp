namespace Milvus.Client;

/// <summary>
/// Binary Field
/// </summary>
public sealed class ArrayFieldData<TElementData> : FieldData<IReadOnlyList<TElementData>>
{
    /// <summary>
    /// Construct an array field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <param name="isDynamic"></param>
    public ArrayFieldData(string fieldName, IEnumerable<IEnumerable<TElementData>> data, bool isDynamic)
        : base(fieldName, data.Select(x => x.ToArray()).ToArray(), MilvusDataType.Array, isDynamic)
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

        foreach (var array in Data)
        {
            switch (ElementType)
            {
                case MilvusDataType.Bool:
                    Grpc.BoolArray boolData = new();
                    boolData.Data.AddRange(array as IEnumerable<bool>);
                    arrayArray.Data.Add(new ScalarField {BoolData = boolData});
                    break;

                case MilvusDataType.Int8:
                    Grpc.IntArray int8Data = new();
                    var sbytes = array as IEnumerable<sbyte> ?? Enumerable.Empty<sbyte>();
                    int8Data.Data.AddRange(sbytes.Select(x => (int) x));
                    arrayArray.Data.Add(new ScalarField {IntData = int8Data});
                    break;

                case MilvusDataType.Int16:
                    Grpc.IntArray int16Data = new();
                    var shorts = array as IEnumerable<short> ?? Enumerable.Empty<short>();
                    int16Data.Data.AddRange(shorts.Select(x=>(int)x));
                    arrayArray.Data.Add(new ScalarField {IntData = int16Data});
                    break;

                case MilvusDataType.Int32:
                    Grpc.IntArray int32Data = new();
                    int32Data.Data.AddRange(array as IEnumerable<int>);
                    arrayArray.Data.Add(new ScalarField {IntData = int32Data});
                    break;

                case MilvusDataType.Int64:
                    Grpc.LongArray int64Data = new();
                    int64Data.Data.AddRange(array as IEnumerable<long>);
                    arrayArray.Data.Add(new ScalarField {LongData = int64Data});
                    break;

                case MilvusDataType.Float:
                    Grpc.FloatArray floatData = new();
                    floatData.Data.AddRange(array as IEnumerable<float>);
                    arrayArray.Data.Add(new ScalarField {FloatData = floatData});
                    break;

                case MilvusDataType.Double:
                    Grpc.DoubleArray doubleData = new();
                    doubleData.Data.AddRange(array as IEnumerable<double>);
                    arrayArray.Data.Add(new ScalarField {DoubleData = doubleData});
                    break;

                case MilvusDataType.String:
                    Grpc.StringArray stringData = new();
                    stringData.Data.AddRange(array as IEnumerable<string>);
                    arrayArray.Data.Add(new ScalarField {StringData = stringData});
                    break;

                case MilvusDataType.VarChar:
                    Grpc.StringArray varcharData = new();
                    varcharData.Data.AddRange(array as IEnumerable<string>);
                    arrayArray.Data.Add(new ScalarField {StringData = varcharData});
                    break;

                case MilvusDataType.Json:
                    Grpc.JSONArray jsonData = new();
                    var enumerable = array as IEnumerable<string> ?? Enumerable.Empty<string>();
                    jsonData.Data.AddRange(enumerable.Select(ByteString.CopyFromUtf8));
                    arrayArray.Data.Add(new ScalarField {JsonData = jsonData});
                    break;
                case MilvusDataType.None:
                    throw new MilvusException($"ElementType Error:{DataType}");
                default:
                    throw new MilvusException($"ElementType Error:{DataType}, not supported");
            }
        }

        return fieldData;

        /*
        int dataCount = Data.Count;
        if (dataCount == 0)
        {
            throw new MilvusException("The number of vectors must be positive.");
        }

        int vectorByteLength = Data[0].Length;
        int totalByteLength = vectorByteLength;
        for (int i = 1; i < dataCount; i++)
        {
            int rowLength = Data[i].Length;
            if (rowLength != vectorByteLength)
            {
                throw new MilvusException("All vectors must have the same dimensionality.");
            }

            checked { totalByteLength += rowLength; }
        }

        byte[] bytes = ArrayPool<byte>.Shared.Rent(totalByteLength);
        int pos = 0;
        for (int i = 0; i < dataCount; i++)
        {
            ReadOnlyMemory<byte> row = Data[i];
            row.Span.CopyTo(bytes.AsSpan(pos, row.Length));
            pos += row.Length;
        }
        Debug.Assert(pos == totalByteLength);

        var result = new Grpc.FieldData
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            Vectors = new Grpc.VectorField
            {
                BinaryVector = ByteString.CopyFrom(bytes.AsSpan(0, totalByteLength)),
                Dim = vectorByteLength * 8,
            }
        };

        ArrayPool<byte>.Shared.Return(bytes);
        */
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
