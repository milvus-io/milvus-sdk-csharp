using Google.Protobuf;
using IO.Milvus.Param;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IO.Milvus.ApiSchema;
using System.Text.Json.Serialization;
using IO.Milvus.Diagnostics;

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
[JsonDerivedType(typeof(Field<Int16>))]
[JsonDerivedType(typeof(Field<int>))]
[JsonDerivedType(typeof(Field<long>))]
[JsonDerivedType(typeof(Field<float>))]
[JsonDerivedType(typeof(Field<double>))]
[JsonDerivedType(typeof(Field<string>))]
public abstract class Field
{
    #region Properties
    /// <summary>
    /// Field name
    /// </summary>
    [JsonPropertyName("field_name")]
    public string FieldName { get; private set; }

    /// <summary>
    /// Row count
    /// </summary>
    [JsonIgnore]
    public abstract int RowCount { get; }

    /// <summary>
    /// <see cref="MilvusDataType"/>
    /// </summary>
    [JsonPropertyName("type")]
    public MilvusDataType DataType { get; protected set; }

    /// <summary>
    /// Convert to a grpc generated field.
    /// </summary>
    /// <returns></returns>
    public abstract Grpc.FieldData ToGrpcFieldData();
    #endregion

    #region Creation
    /// <summary>
    /// Create a field
    /// </summary>
    /// <typeparam name="TData">Data type</typeparam>
    /// <param name="fieldName">Field name</param>
    /// <param name="data">Data in this field</param>
    /// <returns></returns>
    public static Field Create<TData>(
        string fieldName,
        IList<TData> data
        )
    {
        return new Field<TData>()
        {
            FieldName = fieldName,
            Data = data
        };
    }

    public static Field CreateVarChar(
        string fieldName,
        IList<string> data)
    {
        return new Field<string>()
        {
            DataType = MilvusDataType.VarChar,
            FieldName = fieldName,
            Data = data
        };
    }

    /// <summary>
    /// Create a field from <see cref="byte"/> array.
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="bytes">Byte array data</param>
    /// <returns></returns>
    public static Field CreateFromBytes(string fieldName, byte[] bytes)
    {
        ParamUtils.CheckNullEmptyString(fieldName, nameof(FieldName));
        var field = new ByteStringField()
        {
            FieldName = fieldName,
            ByteString = ByteString.CopyFrom(bytes)
        };

        return field;
    }

    /// <summary>
    /// Create a binary vectors
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Field CreateBinaryVectors(string fieldName, List<byte[]> data)
    {
        return new BinaryVectorField()
        {
            FieldName = fieldName,
            Data = data,
        };
    }

    /// <summary>
    /// Create a float vector.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="data">Data</param>
    /// <returns></returns>
    public static Field CreateFloatVector(string fieldName, List<List<float>> data)
    {
        return new FloatVectorField()
        {
            FieldName = fieldName,
            Data = data
        };
    }

    /// <summary>
    /// Create a field from <see cref="ByteString"/>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="byteString"><see cref="ByteString"/></param>
    /// <returns></returns>
    public static Field CreateFromByteString(string name, ByteString byteString)
    {
        ParamUtils.CheckNullEmptyString(name, nameof(FieldName));
        var field = new ByteStringField()
        {
            FieldName = name,
            ByteString = byteString,
        };

        return field;
    }

    /// <summary>
    /// Create a field from stream
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="stream"></param>
    /// <returns>New created field</returns>
    public static Field CreateFromStream(string fieldName, Stream stream)
    {
        ParamUtils.CheckNullEmptyString(fieldName, nameof(FieldName));
        var field = new ByteStringField()
        {
            FieldName = fieldName,
            ByteString = ByteString.FromStream(stream)
        };

        return field;
    }
    #endregion

    internal static Field FromGrpcFieldData(Grpc.FieldData fieldData)
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

                var field = fieldData.Vectors.DataCase switch
                {
                    Grpc.VectorField.DataOneofCase.FloatVector => Field.CreateFloatVector(fieldData.FieldName, floatVectors)
                };

                return field;
            }
            else if (fieldData.Vectors.DataCase == Grpc.VectorField.DataOneofCase.BinaryVector)
            {
                var bytes = fieldData.Vectors.BinaryVector.ToByteArray();

                List<byte[]> byteArray = new();

                using var stream = new MemoryStream(bytes);
                using var reader = new BinaryReader(stream);

                Byte[] subBytes = reader.ReadBytes(bytes.Length);
                while (subBytes?.Any() == true)
                {
                    byteArray.Add(subBytes);
                    subBytes = reader.ReadBytes(bytes.Length);
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
            var field = fieldData.Scalars.DataCase switch
            {
                Grpc.ScalarField.DataOneofCase.BoolData => Field.Create<bool>(fieldData.FieldName, fieldData.Scalars.BoolData.Data),
                Grpc.ScalarField.DataOneofCase.FloatData => Field.Create<float>(fieldData.FieldName, fieldData.Scalars.FloatData.Data),
                Grpc.ScalarField.DataOneofCase.IntData => Field.Create<int>(fieldData.FieldName, fieldData.Scalars.IntData.Data),
                Grpc.ScalarField.DataOneofCase.LongData => Field.Create<long>(fieldData.FieldName, fieldData.Scalars.LongData.Data),
                Grpc.ScalarField.DataOneofCase.StringData => Field.CreateVarChar(fieldData.FieldName, fieldData.Scalars.StringData.Data),
                Grpc.ScalarField.DataOneofCase.ArrayData or Grpc.ScalarField.DataOneofCase.BytesData or Grpc.ScalarField.DataOneofCase.JsonData or Grpc.ScalarField.DataOneofCase.None => throw new NotSupportedException("Array data not support"),
            };
            return field;
        }
        else
        {
            throw new NotSupportedException("Cannot convert None FieldData to Field");
        }
    }
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
    public Field()
    {
        CheckDataType();
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
    public override int RowCount => Data?.Count ?? 0;

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
                    boolData.Data.AddRange(Data as List<bool>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        BoolData = boolData
                    };
                }
                break;
            case MilvusDataType.Int8:
                throw new NotSupportedException("not support in .net");
            case MilvusDataType.Int16:
                {
                    var intData = new Grpc.IntArray();
                    intData.Data.AddRange((Data as List<Int16>).Select(p => (int)p));

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        IntData = intData
                    };
                }
                break;
            case MilvusDataType.Int32:
                {
                    var intData = new Grpc.IntArray();
                    intData.Data.AddRange(Data as List<int>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        IntData = intData
                    };
                }
                break;
            case MilvusDataType.Int64:
                {
                    var longData = new Grpc.LongArray();
                    longData.Data.AddRange(Data as IList<long>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        LongData = longData
                    };
                }
                break;
            case MilvusDataType.Float:
                {
                    var floatData = new Grpc.FloatArray();
                    floatData.Data.AddRange(Data as IList<float>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        FloatData = floatData
                    };
                }
                break;
            case MilvusDataType.Double:
                {
                    var doubleData = new Grpc.DoubleArray();
                    doubleData.Data.AddRange(Data as IList<double>);
                    
                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        DoubleData = doubleData
                    };
                }
                break;
            case MilvusDataType.String:
                {
                    var stringData = new Grpc.StringArray();
                    stringData.Data.AddRange(Data as IList<string>);

                    fieldData.Scalars = new Grpc.ScalarField()
                    {
                        StringData = stringData
                    };
                }
                break;
            case MilvusDataType.VarChar:
                {
                    var stringData = new Grpc.StringArray();
                    stringData.Data.AddRange(Data as IList<string>);

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

    internal void Check()
    {
        Verify.ArgNotNullOrEmpty(FieldName, $"FieldName cannot be null or empth");
        if (Data?.Any() != true)
        {
            throw new MilvusException($"{nameof(Field)}.{nameof(Data)} is empty");
        }
    }

    /// <summary>
    /// Check data type
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    protected void CheckDataType()
    {
        var type = typeof(TData);

        if (type == typeof(bool))
        {
            DataType = MilvusDataType.Bool;
        }
        else if (type == typeof(Int16))
        {
            DataType = MilvusDataType.Int16;
        }
        else if (type == typeof(int) || type == typeof(Int32))
        {
            DataType = MilvusDataType.Int32;
        }
        else if (type == typeof(Int64) || type == typeof(long))
        {
            DataType = MilvusDataType.Int64;
        }
        else if (type == typeof(float))
        {
            DataType = MilvusDataType.Float;
        }
        else if (type == typeof(double))
        {
            DataType = MilvusDataType.Double;
        }
        else if (type == typeof(string))
        {
            DataType = MilvusDataType.String;
        }
        else if (type == typeof(List<float>) || type == typeof(Grpc.FloatArray))
        {
            DataType = MilvusDataType.FloatVector;
        }
        else
        {
            throw new NotSupportedException($"Not Support DataType:{DataType}");
        }
    }
}