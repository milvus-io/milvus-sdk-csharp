using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using IO.Milvus.Diagnostics;
using System.Collections.Specialized;

namespace IO.Milvus;

/// <summary>
/// Represents a milvus field/
/// </summary>
[JsonPolymorphic(
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(BinaryVectorField))]
[JsonDerivedType(typeof(ByteStringField))]
[JsonDerivedType(typeof(FloatVectorField))]
[JsonDerivedType(typeof(Field<bool>))]
[JsonDerivedType(typeof(Field<sbyte>))]
[JsonDerivedType(typeof(Field<short>))]
[JsonDerivedType(typeof(Field<int>))]
[JsonDerivedType(typeof(Field<long>))]
[JsonDerivedType(typeof(Field<float>))]
[JsonDerivedType(typeof(Field<double>))]
[JsonDerivedType(typeof(Field<string>))]
public abstract class Field
{
    /// <summary>
    /// Construct a field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="dataType">Field data type.</param>
    protected Field(string fieldName,MilvusDataType dataType)
    {
        this.FieldName = fieldName;
        this.DataType = dataType;
    }

    #region Properties
    /// <summary>
    /// Field name
    /// </summary>
    [JsonPropertyName("field_name")]
    public string FieldName { get; private set; }

    /// <summary>
    /// Row count.
    /// </summary>
    [JsonIgnore]
    public abstract long RowCount { get; protected set; }

    /// <summary>
    /// Field id.
    /// </summary>
    [JsonPropertyName("field_id")]
    public long FieldId { get; internal set; }

    /// <summary>
    /// <see cref="MilvusDataType"/>
    /// </summary>
    [JsonPropertyName("type")]
    public MilvusDataType DataType { get; protected set; }
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
        if (fieldData.FieldCase == Grpc.FieldData.FieldOneofCase.Vectors)
        {
            int dim = (int)fieldData.Vectors.Dim;

            if (fieldData.Vectors.DataCase == Grpc.VectorField.DataOneofCase.FloatVector)
            {
                List<List<float>> floatVectors = new();
                for (int i = 0; i < fieldData.Vectors.FloatVector.Data.Count; i++)
                {
                    var list = new List<float>(fieldData.Vectors.FloatVector.Data.Skip(i).Take(dim));
                    floatVectors.Add(list);
                    i += dim;
                }
                var vector = fieldData.Vectors.FloatVector.Data.ToList();

                var field = Field.CreateFloatVector(fieldData.FieldName, floatVectors);

                return field;
            }
            else if (fieldData.Vectors.DataCase == Grpc.VectorField.DataOneofCase.BinaryVector)
            {
                var bytes = fieldData.Vectors.BinaryVector.ToByteArray();

                List<byte[]> byteArray = new();

                using var stream = new MemoryStream(bytes);
                using var reader = new BinaryReader(stream);

                Byte[] subBytes = reader.ReadBytes(dim);
                while (subBytes?.Any() == true)
                {
                    byteArray.Add(subBytes);
                    subBytes = reader.ReadBytes(dim);
                }

                return Field.CreateBinaryVectors(fieldData.FieldName, byteArray);
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
                _ => throw new NotSupportedException("Array data not support"),
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
        var type = typeof(TDataType);
        MilvusDataType dataType = MilvusDataType.Double;

        if (type == typeof(bool))
        {
            dataType = MilvusDataType.Bool;
        }
        else if(type == typeof(sbyte))
        {
            dataType = MilvusDataType.Int8;
        }
        else if (type == typeof(Int16))
        {
            dataType = MilvusDataType.Int16;
        }
        else if (type == typeof(int) || type == typeof(Int32))
        {
            dataType = MilvusDataType.Int32;
        }
        else if (type == typeof(Int64) || type == typeof(long))
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
    /// <item><see cref="Int16"/> : int16 <see cref="MilvusDataType.Int16"/></item>
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
    /// <returns></returns>
    public static Field<string> CreateVarChar(
        string fieldName,
        IList<string> data)
    {
        return new Field<string>(fieldName, data,MilvusDataType.VarChar);
    }

    /// <summary>
    /// Create a field from <see cref="byte"/> array.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="bytes">Byte array data.</param>
    /// <param name="dimension">Dimension of data.</param>
    /// <returns></returns>
    public static BinaryVectorField CreateFromBytes(string fieldName, byte[] bytes,long dimension)
    {
        Verify.ArgNotNullOrEmpty(fieldName, nameof(FieldName));

        List<byte[]> byteArray = new();

        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);

        Byte[] subBytes = reader.ReadBytes((int)dimension);
        while (subBytes?.Any() == true)
        {
            byteArray.Add(subBytes);
            subBytes = reader.ReadBytes((int)dimension);
        }

        var field = new BinaryVectorField(fieldName, byteArray) ;
        return field;
    }

    /// <summary>
    /// Create a binary vectors
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static BinaryVectorField CreateBinaryVectors(string fieldName, IList<byte[]> data)
    {
        Verify.ArgNotNullOrEmpty(fieldName, nameof(FieldName));
        var field = new BinaryVectorField(fieldName, data);
        return field;
    }

    /// <summary>
    /// Create a float vector.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="data">Data</param>
    /// <returns></returns>
    public static FloatVectorField CreateFloatVector(string fieldName, List<List<float>> data)
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
            var subVector = floatVector.GetRange(i, (int)dimension);
            floatVectors.Add(subVector);
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
    public static ByteStringField CreateFromByteString(string fieldName, ByteString byteString,long dimension)
    {
        Verify.ArgNotNullOrEmpty(fieldName, nameof(FieldName));
        var field = new ByteStringField(fieldName, byteString, dimension);

        return field;
    }

    /// <summary>
    /// Create a field from stream
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="stream"></param>
    /// <param name="dimension">Dimension of data</param>
    /// <returns>New created field</returns>
    public static Field CreateFromStream(string fieldName, Stream stream,long dimension)
    {
        Verify.ArgNotNullOrEmpty(fieldName, "Field name cannot be null or empty.");
        var field = new ByteStringField(fieldName, ByteString.FromStream(stream), dimension);

        return field;
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
    public Field(string fieldName, IList<TData> data):
        base(fieldName,EnsureDataType<TData>())
    {
        Data = data;
    }

    /// <summary>
    /// Construct a field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <param name="milvusDataType">Milvus data type.</param>
    public Field(
        string fieldName,
        IList<TData> data,
        MilvusDataType milvusDataType) : 
        base(fieldName, milvusDataType)
    {
        Data = data;
    }

    /// <summary>
    /// Vector data
    /// </summary>
    [JsonPropertyName("field")]
    public IList<TData> Data { get; set; }

    /// <summary>
    /// Row count
    /// </summary>
    [JsonIgnore]
    public override long RowCount
    {
        get
        {
            return Data?.Count ?? 0;
        }
        protected set { }
    }

    ///<inheritdoc/>
    public override Grpc.FieldData ToGrpcFieldData()
    {
        Check();

        var fieldData = new Grpc.FieldData()
        {
            FieldName = FieldName,
            Type = (Grpc.DataType)DataType
        };

        switch (DataType)
        {
            case MilvusDataType.None:
                throw new MilvusException($"DataType Error:{DataType}");
            case MilvusDataType.Bool:
                {
                    var boolData = new Grpc.BoolArray();
                    boolData.Data.AddRange(Data as IEnumerable<bool>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        BoolData = boolData
                    };
                }
                break;
            case MilvusDataType.Int8:
                {
                    var intData = new Grpc.IntArray();
                    intData.Data.AddRange((Data as IEnumerable<sbyte>).Select(p => (int)p));

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        IntData = intData
                    };
                }
                break;
            case MilvusDataType.Int16:
                {
                    var intData = new Grpc.IntArray();
                    intData.Data.AddRange((Data as IEnumerable<Int16>).Select(p => (int)p));

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        IntData = intData
                    };
                }
                break;
            case MilvusDataType.Int32:
                {
                    var intData = new Grpc.IntArray();
                    intData.Data.AddRange(Data as IEnumerable<int>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        IntData = intData
                    };
                }
                break;
            case MilvusDataType.Int64:
                {
                    var longData = new Grpc.LongArray();
                    longData.Data.AddRange(Data as IEnumerable<long>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        LongData = longData
                    };
                }
                break;
            case MilvusDataType.Float:
                {
                    var floatData = new Grpc.FloatArray();
                    floatData.Data.AddRange(Data as IEnumerable<float>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        FloatData = floatData
                    };
                }
                break;
            case MilvusDataType.Double:
                {
                    var doubleData = new Grpc.DoubleArray();
                    doubleData.Data.AddRange(Data as IEnumerable<double>);
                    
                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        DoubleData = doubleData
                    };
                }
                break;
            case MilvusDataType.String:
                {
                    var stringData = new Grpc.StringArray();
                    stringData.Data.AddRange(Data as IEnumerable<string>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        StringData = stringData
                    };
                }
                break;
            case MilvusDataType.VarChar:
                {
                    var stringData = new Grpc.StringArray();
                    stringData.Data.AddRange(Data as IEnumerable<string>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        StringData = stringData
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
        Verify.ArgNotNullOrEmpty(FieldName, $"FieldName cannot be null or empty");
        if (Data?.Any() != true)
        {
            throw new MilvusException($"{nameof(Field)}.{nameof(Data)} is empty");
        }
    }
}