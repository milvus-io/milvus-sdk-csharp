using System.Diagnostics;
using Google.Protobuf.Collections;

namespace IO.Milvus;

/// <summary>
/// Represents a milvus field/
/// </summary>
public abstract class FieldData
{
    /// <summary>
    /// Construct a field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="dataType">Field data type.</param>
    /// <param name="isDynamic"></param>
    protected FieldData(string fieldName, MilvusDataType dataType, bool isDynamic = false)
    {
        FieldName = fieldName;
        DataType = dataType;
        IsDynamic = isDynamic;
    }

    /// <summary>
    /// Field name
    /// </summary>
    public string FieldName { get; private set; }

    /// <summary>
    /// Row count.
    /// </summary>
    public abstract long RowCount { get; protected set; }

    /// <summary>
    /// Field id.
    /// </summary>
    public long FieldId { get; internal set; }

    /// <summary>
    /// <see cref="MilvusDataType"/>
    /// </summary>
    public MilvusDataType DataType { get; protected set; }

    /// <summary>
    /// Is dynamic.
    /// </summary>
    public bool IsDynamic { get; private set; }

    /// <summary>
    /// Get string data.
    /// </summary>
    /// <returns>string data.</returns>
    public override string ToString()
        => $"{{{nameof(FieldName)}: {FieldName}, {nameof(DataType)}: {DataType}, {nameof(RowCount)}: {RowCount}}}";

    /// <summary>
    /// Convert to a grpc generated field.
    /// </summary>
    /// <returns></returns>
    internal abstract Grpc.FieldData ToGrpcFieldData();

    /// <summary>
    /// Convert to field from <see cref="Grpc.FieldData"/>.
    /// </summary>
    /// <param name="fieldData">Field data.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static FieldData FromGrpcFieldData(Grpc.FieldData fieldData)
    {
        Verify.NotNull(fieldData);

        switch (fieldData.FieldCase)
        {
            case Grpc.FieldData.FieldOneofCase.Vectors:
                int dim = (int)fieldData.Vectors.Dim;

                switch (fieldData.Vectors.DataCase)
                {
                    case Grpc.VectorField.DataOneofCase.FloatVector:
                    {
                        RepeatedField<float> grpcData = fieldData.Vectors.FloatVector.Data;
                        int totalElementCount = grpcData.Count;
                        Debug.Assert(totalElementCount % dim == 0);
                        int vectorCount = totalElementCount / dim;

                        ReadOnlyMemory<float>[] vectors = new ReadOnlyMemory<float>[vectorCount];

                        for (int i = 0; i < vectorCount; i++)
                        {
                            float[] vector = new float[dim];
                            int vectorStart = i * dim;

                            for (int j = 0; j < dim; j++)
                            {
                                vector[j] = grpcData[vectorStart + j];
                            }

                            vectors[i] = vector;
                        }

                        return CreateFloatVector(fieldData.FieldName, vectors);
                    }

                    case Grpc.VectorField.DataOneofCase.BinaryVector:
                        return CreateFromBytes(fieldData.FieldName, fieldData.Vectors.BinaryVector.Span, dim);

                    default:
                        throw new NotSupportedException("VectorField.DataOneofCase.None not support");
                }

            case Grpc.FieldData.FieldOneofCase.Scalars:
                return fieldData.Scalars.DataCase switch
                {
                    Grpc.ScalarField.DataOneofCase.BoolData
                        => Create(fieldData.FieldName, fieldData.Scalars.BoolData.Data),
                    Grpc.ScalarField.DataOneofCase.FloatData
                        => Create(fieldData.FieldName, fieldData.Scalars.FloatData.Data),
                    Grpc.ScalarField.DataOneofCase.IntData
                        => Create(fieldData.FieldName, fieldData.Scalars.IntData.Data),
                    Grpc.ScalarField.DataOneofCase.LongData
                        => Create(fieldData.FieldName, fieldData.Scalars.LongData.Data),
                    Grpc.ScalarField.DataOneofCase.StringData
                        => CreateVarChar(fieldData.FieldName, fieldData.Scalars.StringData.Data),
                    Grpc.ScalarField.DataOneofCase.JsonData
                        => CreateJson(fieldData.FieldName, fieldData.Scalars.JsonData.Data
                            .Select(p => p.ToStringUtf8()).ToList()),

                    _ => throw new NotSupportedException($"{fieldData.Scalars.DataCase} not support"),
                };

            default:
                throw new NotSupportedException("Cannot convert None FieldData to Field");
        }
    }

    /// <summary>
    /// Check data type
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    internal static MilvusDataType EnsureDataType<TDataType>()
    {
        Type type = typeof(TDataType);
        MilvusDataType dataType = MilvusDataType.Double;

        if (type == typeof(bool))
        {
            dataType = MilvusDataType.Bool;
        }
        else if (type == typeof(sbyte))
        {
            dataType = MilvusDataType.Int8;
        }
        else if (type == typeof(short))
        {
            dataType = MilvusDataType.Int16;
        }
        else if (type == typeof(int))
        {
            dataType = MilvusDataType.Int32;
        }
        else if (type == typeof(long))
        {
            dataType = MilvusDataType.Int64;
        }
        else if (type == typeof(float))
        {
            dataType = MilvusDataType.Float;
        }
        else if (type == typeof(double))
        {
            dataType = MilvusDataType.Double;
        }
        else if (type == typeof(string))
        {
            dataType = MilvusDataType.VarChar;
            //dataType = MilvusDataType.String;Not support now.
        }
        // TODO: Support arbitrary IList<float> since that's what Field supports
        else if (type == typeof(List<float>) || type == typeof(Grpc.FloatArray))
        {
            dataType = MilvusDataType.FloatVector;
        }
        else
        {
            throw new NotSupportedException($"Not Support DataType:{dataType}");
        }

        return dataType;
    }

    /// <summary>
    /// Create a field
    /// </summary>
    /// <typeparam name="TData">
    /// Data type: If you use string , the data type will be <see cref="MilvusDataType.VarChar"/>
    /// <list type="bullet">
    /// <item><see cref="bool"/> : bool <see cref="MilvusDataType.Bool"/></item>
    /// <item><see cref="sbyte"/> : int8 <see cref="MilvusDataType.Int8"/></item>
    /// <item><see cref="short"/> : int16 <see cref="MilvusDataType.Int16"/></item>
    /// <item><see cref="int"/> : int32 <see cref="MilvusDataType.Int32"/></item>
    /// <item><see cref="long"/> : int64 <see cref="MilvusDataType.Int64"/></item>
    /// <item><see cref="float"/> : float <see cref="MilvusDataType.Float"/></item>
    /// <item><see cref="double"/> : double <see cref="MilvusDataType.Double"/></item>
    /// <item><see cref="string"/> : string <see cref="MilvusDataType.VarChar"/></item>
    /// </list>
    /// </typeparam>
    /// <param name="fieldName">Field name</param>
    /// <param name="data">Data in this field</param>
    /// <returns></returns>
    public static FieldData<TData> Create<TData>(string fieldName, IReadOnlyList<TData> data)
        => new(fieldName, data);

    /// <summary>
    /// Create a varchar field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="data">Data.</param>
    /// <param name="isDynamic"></param>
    /// <returns></returns>
    public static FieldData<string> CreateVarChar(
        string fieldName,
        IReadOnlyList<string> data,
        bool isDynamic = false)
        => new(fieldName, data, MilvusDataType.VarChar, isDynamic);

    /// <summary>
    /// Create a field from <see cref="byte"/> array.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="bytes">Byte array data.</param>
    /// <param name="dimension">Dimension of data.</param>
    /// <returns></returns>
    public static BinaryVectorFieldData CreateFromBytes(string fieldName, ReadOnlySpan<byte> bytes, long dimension)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.GreaterThan(dimension, 0);

        List<ReadOnlyMemory<byte>> byteArray = new((int)Math.Ceiling((double)bytes.Length / dimension));

        while (bytes.Length > dimension)
        {
            byteArray.Add(bytes.Slice(0, (int)dimension).ToArray());
            bytes = bytes.Slice((int)dimension);
        }

        if (!bytes.IsEmpty)
        {
            byteArray.Add(bytes.ToArray());
        }

        return new BinaryVectorFieldData(fieldName, byteArray);
    }

    /// <summary>
    /// Create a binary vectors
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static BinaryVectorFieldData CreateBinaryVectors(string fieldName, IReadOnlyList<ReadOnlyMemory<byte>> data)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        BinaryVectorFieldData fieldData = new(fieldName, data);
        return fieldData;
    }

    /// <summary>
    /// Create a float vector.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="data">Data</param>
    /// <returns></returns>
    public static FloatVectorFieldData CreateFloatVector(string fieldName, IReadOnlyList<ReadOnlyMemory<float>> data)
        => new(fieldName, data);

    /// <summary>
    /// Create a field from stream
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="stream"></param>
    /// <param name="dimension">Dimension of data</param>
    /// <returns>New created field</returns>
    public static FieldData CreateFromStream(string fieldName, Stream stream, long dimension)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        ByteStringFieldData fieldData = new(fieldName, ByteString.FromStream(stream), dimension);

        return fieldData;
    }

    /// <summary>
    /// Create json field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="json">json field.</param>
    /// <param name="isDynamic"></param>
    /// <returns></returns>
    public static FieldData CreateJson(string fieldName, IReadOnlyList<string> json, bool isDynamic = false)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        return new FieldData<string>(fieldName, json, MilvusDataType.Json, isDynamic);
    }
}

/// <summary>
/// Milvus Field
/// </summary>
/// <typeparam name="TData"></typeparam>
public class FieldData<TData> : FieldData
{
    /// <summary>
    /// Construct a field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <param name="isDynamic"></param>
    public FieldData(string fieldName, IReadOnlyList<TData> data, bool isDynamic = false)
        : base(fieldName, EnsureDataType<TData>(), isDynamic)
        => Data = data;

    /// <summary>
    /// Construct a field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <param name="milvusDataType">Milvus data type.</param>
    /// <param name="isDynamic"></param>
    public FieldData(string fieldName, IReadOnlyList<TData> data, MilvusDataType milvusDataType, bool isDynamic)
        : base(fieldName, milvusDataType, isDynamic)
        => Data = data;

    /// <summary>
    /// Vector data
    /// </summary>
    public IReadOnlyList<TData> Data { get; set; }

    /// <summary>
    /// Row count
    /// </summary>
    public override long RowCount
    {
        get => Data.Count;
        protected set { }
    }

    /// <inheritdoc />
    internal override Grpc.FieldData ToGrpcFieldData()
    {
        Check();

        Grpc.FieldData fieldData = new()
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType,
            IsDynamic = IsDynamic
        };

        switch (DataType)
        {
            case MilvusDataType.Bool:
                Grpc.BoolArray boolData = new();
                boolData.Data.AddRange(Data as IEnumerable<bool>);
                fieldData.Scalars = new Grpc.ScalarField { BoolData = boolData };
                break;

            case MilvusDataType.Int8:
                Grpc.IntArray int8Data = new();
                foreach (sbyte i in (IEnumerable<sbyte>)Data)
                {
                    int8Data.Data.Add(i);
                }
                fieldData.Scalars = new Grpc.ScalarField { IntData = int8Data };
                break;

            case MilvusDataType.Int16:
                Grpc.IntArray int16Data = new();
                foreach (short i in (IEnumerable<short>)Data)
                {
                    int16Data.Data.Add(i);
                }
                fieldData.Scalars = new Grpc.ScalarField { IntData = int16Data };
                break;

            case MilvusDataType.Int32:
                Grpc.IntArray int32Data = new();
                int32Data.Data.AddRange((IEnumerable<int>)Data);
                fieldData.Scalars = new Grpc.ScalarField { IntData = int32Data };
                break;

            case MilvusDataType.Int64:
                Grpc.LongArray int64Data = new();
                int64Data.Data.AddRange((IEnumerable<long>)Data);
                fieldData.Scalars = new Grpc.ScalarField { LongData = int64Data };
                break;

            case MilvusDataType.Float:
                Grpc.FloatArray floatData = new();
                floatData.Data.AddRange((IEnumerable<float>)Data);
                fieldData.Scalars = new Grpc.ScalarField { FloatData = floatData };
                break;

            case MilvusDataType.Double:
                Grpc.DoubleArray doubleData = new();
                doubleData.Data.AddRange((IEnumerable<double>)Data);
                fieldData.Scalars = new Grpc.ScalarField { DoubleData = doubleData };
                break;

            case MilvusDataType.String:
                Grpc.StringArray stringData = new();
                stringData.Data.AddRange((IEnumerable<string>)Data);
                fieldData.Scalars = new Grpc.ScalarField { StringData = stringData };
                break;

            case MilvusDataType.VarChar:
                Grpc.StringArray varcharData = new();
                varcharData.Data.AddRange((IEnumerable<string>)Data);
                fieldData.Scalars = new Grpc.ScalarField { StringData = varcharData };
                break;

            case MilvusDataType.Json:
                Grpc.JSONArray jsonData = new();
                foreach (string jsonString in (IList<string>)Data)
                {
                    jsonData.Data.Add(ByteString.CopyFromUtf8(jsonString));
                }
                fieldData.Scalars = new Grpc.ScalarField { JsonData = jsonData, };
                break;

            case MilvusDataType.None:
                throw new MilvusException($"DataType Error:{DataType}");
            default:
                throw new MilvusException($"DataType Error:{DataType}, not supported");
        }

        return fieldData;
    }

    /// <summary>
    /// Return string value of <see cref="FieldData{TData}"/>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
        => $"Field: {{{nameof(FieldName)}: {FieldName}, {nameof(DataType)}: {DataType}, {nameof(Data)}: {Data?.Count}, {nameof(RowCount)}: {RowCount}}}";

    private void Check()
    {
        Verify.NotNullOrWhiteSpace(FieldName);

        if (Data.Any() != true)
        {
            throw new MilvusException($"{nameof(FieldData)}.{nameof(Data)} is empty");
        }
    }
}
