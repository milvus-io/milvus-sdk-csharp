using Google.Protobuf;
using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Represents a milvus field/
/// </summary>
public abstract class Field
{
    /// <summary>
    /// Construct a field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="dataType">Field data type.</param>
    /// <param name="isDynamic"></param>
    protected Field(string fieldName, MilvusDataType dataType, bool isDynamic = false)
    {
        FieldName = fieldName;
        DataType = dataType;
        IsDynamic = isDynamic;
    }

    #region Properties
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
    public bool IsDynamic { get; set; }
    #endregion

    /// <summary>
    /// Get string data.
    /// </summary>
    /// <returns>string data.</returns>
    public override string ToString()
    {
        return $"{{{nameof(FieldName)}: {FieldName}, {nameof(DataType)}: {DataType}, {nameof(RowCount)}: {RowCount}}}";
    }

    #region Converter
    /// <summary>
    /// Convert to a grpc generated field.
    /// </summary>
    /// <returns></returns>
    public abstract Grpc.FieldData ToGrpcFieldData();

    /// <summary>
    /// Convert to field from <see cref="Grpc.FieldData"/>.
    /// </summary>
    /// <param name="fieldData">Field data.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static Field FromGrpcFieldData(Grpc.FieldData fieldData)
    {
        Verify.NotNull(fieldData);

        if (fieldData.FieldCase == Grpc.FieldData.FieldOneofCase.Vectors)
        {
            int dim = (int)fieldData.Vectors.Dim;

            if (fieldData.Vectors.DataCase == Grpc.VectorField.DataOneofCase.FloatVector)
            {
                List<List<float>> floatVectors = new();

                for (int i = 0; i < fieldData.Vectors.FloatVector.Data.Count; i += dim)
                {
                    List<float> list = new(fieldData.Vectors.FloatVector.Data.Skip(i).Take(dim));
                    floatVectors.Add(list);
                }

                FloatVectorField field = Field.CreateFloatVector(fieldData.FieldName, floatVectors);

                return field;
            }
            else if (fieldData.Vectors.DataCase == Grpc.VectorField.DataOneofCase.BinaryVector)
            {
                return CreateFromBytes(fieldData.FieldName, fieldData.Vectors.BinaryVector.Span, dim);
            }
            else
            {
                throw new NotSupportedException("VectorField.DataOneofCase.None not support");
            }

        }
        else if (fieldData.FieldCase == Grpc.FieldData.FieldOneofCase.Scalars)
        {
            Field field = fieldData.Scalars.DataCase switch
            {
                Grpc.ScalarField.DataOneofCase.BoolData => Field.Create<bool>(fieldData.FieldName, fieldData.Scalars.BoolData.Data),
                Grpc.ScalarField.DataOneofCase.FloatData => Field.Create<float>(fieldData.FieldName, fieldData.Scalars.FloatData.Data),
                Grpc.ScalarField.DataOneofCase.IntData => Field.Create<int>(fieldData.FieldName, fieldData.Scalars.IntData.Data),
                Grpc.ScalarField.DataOneofCase.LongData => Field.Create<long>(fieldData.FieldName, fieldData.Scalars.LongData.Data),
                Grpc.ScalarField.DataOneofCase.StringData => Field.CreateVarChar(fieldData.FieldName, fieldData.Scalars.StringData.Data),
                Grpc.ScalarField.DataOneofCase.JsonData => Field.CreateJson(fieldData.FieldName, fieldData.Scalars.JsonData.Data.Select(p => p.ToStringUtf8()).ToList()),
                _ => throw new NotSupportedException($"{fieldData.Scalars.DataCase} not support"),
            };
            return field;
        }
        else
        {
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
    #endregion

    #region Creation
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
    public static Field<TData> Create<TData>(
        string fieldName,
        IList<TData> data
        )
    {
        return new Field<TData>(fieldName, data);
    }

    /// <summary>
    /// Create a varchar field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="data">Data.</param>
    /// <param name="isDynamic"></param>
    /// <returns></returns>
    public static Field<string> CreateVarChar(
        string fieldName,
        IList<string> data,
        bool isDynamic = false)
    {
        return new Field<string>(fieldName, data, MilvusDataType.VarChar, isDynamic);
    }

    /// <summary>
    /// Create a field from <see cref="byte"/> array.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="bytes">Byte array data.</param>
    /// <param name="dimension">Dimension of data.</param>
    /// <returns></returns>
    public static BinaryVectorField CreateFromBytes(string fieldName, ReadOnlySpan<byte> bytes, long dimension)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        Verify.GreaterThan(dimension, 0);

        List<byte[]> byteArray = new((int)Math.Ceiling((double)bytes.Length / dimension));

        while (bytes.Length > dimension)
        {
            byteArray.Add(bytes.Slice(0, (int)dimension).ToArray());
            bytes = bytes.Slice((int)dimension);
        }

        if (!bytes.IsEmpty)
        {
            byteArray.Add(bytes.ToArray());
        }

        return new BinaryVectorField(fieldName, byteArray);
    }

    /// <summary>
    /// Create a binary vectors
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static BinaryVectorField CreateBinaryVectors(string fieldName, IList<byte[]> data)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        BinaryVectorField field = new(fieldName, data);
        return field;
    }

    /// <summary>
    /// Create a float vector.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="data">Data</param>
    /// <returns></returns>
    public static FloatVectorField CreateFloatVector(string fieldName, IList<List<float>> data)
    {
        return new FloatVectorField(fieldName, data);
    }

    /// <summary>
    /// Create a float vector.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="floatVector">Float vector.</param>
    /// <param name="dimension">Dimension.</param>
    /// <returns></returns>
    internal static FloatVectorField CreateFloatVector(string fieldName, List<float> floatVector, long dimension)
    {
        List<List<float>> floatVectors = new();

        for (int i = 0; i < floatVector.Count; i += (int)dimension)
        {
            floatVectors.Add(floatVector.GetRange(i, (int)dimension));
        }

        return new FloatVectorField(fieldName, floatVectors);
    }

    /// <summary>
    /// Create a field from <see cref="ByteString"/>
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="byteString"><see cref="ByteString"/></param>
    /// <param name="dimension">Dimension of this field.</param>
    /// <returns></returns>
    public static ByteStringField CreateFromByteString(string fieldName, ByteString byteString, long dimension)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        ByteStringField field = new(fieldName, byteString, dimension);

        return field;
    }

    /// <summary>
    /// Create a field from stream
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="stream"></param>
    /// <param name="dimension">Dimension of data</param>
    /// <returns>New created field</returns>
    public static Field CreateFromStream(string fieldName, Stream stream, long dimension)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        ByteStringField field = new ByteStringField(fieldName, ByteString.FromStream(stream), dimension);

        return field;
    }

    /// <summary>
    /// Create json field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="json">json field.</param>
    /// <param name="isDynamic"></param>
    /// <returns></returns>
    public static Field CreateJson(string fieldName, IList<string> json, bool isDynamic = false)
    {
        Verify.NotNullOrWhiteSpace(fieldName);
        return new Field<string>(fieldName, json, MilvusDataType.Json, isDynamic);
    }
    #endregion
}

/// <summary>
/// Milvus Field
/// </summary>
/// <typeparam name="TData"></typeparam>
public class Field<TData> : Field
{
    /// <summary>
    /// Construct a field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <param name="isDynamic"></param>
    public Field(string fieldName, IList<TData> data, bool isDynamic = false) :
        base(fieldName, EnsureDataType<TData>(), isDynamic)
    {
        Data = data;
    }

    /// <summary>
    /// Construct a field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <param name="milvusDataType">Milvus data type.</param>
    /// <param name="isDynamic"></param>
    public Field(
        string fieldName,
        IList<TData> data,
        MilvusDataType milvusDataType,
        bool isDynamic) :
        base(fieldName, milvusDataType, isDynamic)
    {
        Data = data;
    }

    /// <summary>
    /// Vector data
    /// </summary>
    public IList<TData> Data { get; set; }

    /// <summary>
    /// Row count
    /// </summary>
    public override long RowCount
    {
        get
        {
            return Data?.Count ?? 0;
        }
        protected set { }
    }

    /// <inheritdoc />
    public override Grpc.FieldData ToGrpcFieldData()
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
            case MilvusDataType.None:
                throw new MilvusException($"DataType Error:{DataType}");
            case MilvusDataType.Bool:
                {
                    Grpc.BoolArray boolData = new();
                    boolData.Data.AddRange(Data as IEnumerable<bool>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        BoolData = boolData
                    };
                }
                break;
            case MilvusDataType.Int8:
                {
                    Grpc.IntArray intData = new();
                    intData.Data.AddRange(((IEnumerable<sbyte>)Data).Select(static p => (int)p));

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        IntData = intData
                    };
                }
                break;
            case MilvusDataType.Int16:
                {
                    Grpc.IntArray intData = new();
                    intData.Data.AddRange(((IEnumerable<short>)Data).Select(static p => (int)p));

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        IntData = intData
                    };
                }
                break;
            case MilvusDataType.Int32:
                {
                    Grpc.IntArray intData = new();
                    intData.Data.AddRange((IEnumerable<int>)Data);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        IntData = intData
                    };
                }
                break;
            case MilvusDataType.Int64:
                {
                    Grpc.LongArray longData = new();
                    longData.Data.AddRange((IEnumerable<long>)Data);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        LongData = longData
                    };
                }
                break;
            case MilvusDataType.Float:
                {
                    Grpc.FloatArray floatData = new();
                    floatData.Data.AddRange((IEnumerable<float>)Data);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        FloatData = floatData
                    };
                }
                break;
            case MilvusDataType.Double:
                {
                    Grpc.DoubleArray doubleData = new();
                    doubleData.Data.AddRange((IEnumerable<double>)Data);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        DoubleData = doubleData
                    };
                }
                break;
            case MilvusDataType.String:
                {
                    Grpc.StringArray stringData = new();
                    stringData.Data.AddRange((IEnumerable<string>)Data);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        StringData = stringData
                    };
                }
                break;
            case MilvusDataType.VarChar:
                {
                    Grpc.StringArray stringData = new();
                    stringData.Data.AddRange((IEnumerable<string>)Data);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        StringData = stringData
                    };
                }
                break;
            case MilvusDataType.Json:
                {
                    Grpc.JSONArray jsonData = new Grpc.JSONArray();
                    foreach (string jsonString in (IList<string>)Data)
                    {
                        jsonData.Data.Add(ByteString.CopyFromUtf8(jsonString));
                    }
                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        JsonData = jsonData,
                    };
                }
                break;
            default:
                throw new MilvusException($"DataType Error:{DataType}, not supported");
        }

        return fieldData;
    }

    /// <summary>
    /// Return string value of <see cref="Field{TData}"/>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"Field: {{{nameof(FieldName)}: {FieldName}, {nameof(DataType)}: {DataType}, {nameof(Data)}: {Data?.Count}, {nameof(RowCount)}: {RowCount}}}";
    }

    internal void Check()
    {
        Verify.NotNullOrWhiteSpace(FieldName);
        if (Data?.Any() != true)
        {
            throw new MilvusException($"{nameof(Field)}.{nameof(Data)} is empty");
        }
    }
}
