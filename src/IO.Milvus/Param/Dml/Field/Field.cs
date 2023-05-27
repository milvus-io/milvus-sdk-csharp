using Google.Protobuf;
using Google.Protobuf.Collections;
using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IO.Milvus.Param.Dml
{
    /// <summary>
    /// Field in milvus.
    /// </summary>
    public abstract class Field
    {
        /// <summary>
        /// Create a field from a list of data.
        /// </summary>
        /// <typeparam name="TData">
        /// Data type:
        /// <list type="bullet">
        /// <item><see cref="bool"/> : int32</item>
        /// <item><see cref="short"/> : int8</item>
        /// <item><see cref="Int16"/> : int8</item>
        /// <item><see cref="int"/> : int32</item>
        /// <item><see cref="long"/> : int64</item>
        /// <item><see cref="float"/> : float</item>
        /// <item><see cref="double"/> : double</item>
        /// <item><see cref="string"/> : string</item>
        /// </list>
        /// </typeparam>
        /// <param name="name">Field name.</param>
        /// <param name="data">data</param>
        /// <returns></returns>
        public static Field Create<TData>(
            string name,
            List<TData> data
            )
        {
            return new Field<TData>()
            {
                FieldName = name,
                Data = data
            };
        }

        /// <summary>
        /// Create a binary field from bytes.
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="bytes">bytes.</param>
        /// <returns>A new created field.</returns>
        public static Field CreateFromBytes(string name, byte[] bytes)
        {
            ParamUtils.CheckNullEmptyString(name, nameof(FieldName));
            var field = new ByteStringField()
            {
                FieldName = name,
                Bytes = ByteString.CopyFrom(bytes)
            };

            return field;
        }

        public static Field CreateBinaryVectors(string name,List<List<float>> data)
        {
            return new BinaryVectorField(name, data);
        }

        public static Field CreateFromByteString(string name, ByteString bytestr)
        {
            ParamUtils.CheckNullEmptyString(name, nameof(FieldName));
            var field = new ByteStringField()
            {
                FieldName = name,
                Bytes = bytestr
            };

            return field;
        }

        public static Field CreateFromStream(string name, Stream stream)
        {
            ParamUtils.CheckNullEmptyString(name, nameof(FieldName));
            var field = new ByteStringField()
            {
                FieldName = name,
                Bytes = ByteString.FromStream(stream)
            };

            return field;
        }

        public string FieldName { get; set; }

        public abstract int RowCount { get; }

        public DataType DataType { get; protected set; }

        public abstract FieldData ToGrpcFieldData();
    }

    public class Field<TData>:Field
    {
        public Field()
        {
            CheckDataType();
        }

        public List<TData> Data { get; set; }

        public override int RowCount => Data?.Count ?? 0;

        public override FieldData ToGrpcFieldData()
        {
            Check();

            var fieldData = new FieldData()
            {
                FieldName = FieldName,
                Type = DataType
            };

            switch (DataType)
            {
                case DataType.None:
                    throw new MilvusException($"DataType Error:{DataType}");
                case DataType.Bool:
                    {
                        var boolData = new BoolArray();
                        boolData.Data.AddRange(Data as List<bool>);

                        fieldData.Scalars = new ScalarField()
                        {
                            BoolData = boolData
                        };
                    }
                    break;
                case DataType.Int8:
                    {
                        var intData = new IntArray();
                        intData.Data.AddRange((Data as List<short>).Select(p => (int)p));

                        fieldData.Scalars = new ScalarField()
                        {
                            IntData = intData
                        };
                    }
                    break;
                case DataType.Int16:
                    {
                        var intData = new IntArray();
                        intData.Data.AddRange((Data as List<Int16>).Select(p => (int)p));

                        fieldData.Scalars = new ScalarField()
                        {
                            IntData = intData
                        };
                    }
                    break;
                case DataType.Int32:
                    {
                        var intData = new IntArray();
                        intData.Data.AddRange(Data as List<int>);

                        fieldData.Scalars = new ScalarField()
                        {
                            IntData = intData
                        };
                    }
                    break;
                case DataType.Int64:
                    {
                        var longData = new LongArray();
                        longData.Data.AddRange(Data as List<long>);

                        fieldData.Scalars = new ScalarField()
                        {
                            LongData = longData
                        };
                    }
                    break;
                case DataType.Float:
                    {
                        var floatData = new FloatArray();
                        floatData.Data.AddRange(Data as List<float>);

                        fieldData.Scalars = new ScalarField()
                        {
                            FloatData = floatData
                        };
                    }
                    break;
                case DataType.Double:
                    {
                        var doubleData = new DoubleArray();
                        doubleData.Data.AddRange(Data as List<double>);

                        fieldData.Scalars = new ScalarField()
                        {
                            DoubleData = doubleData
                        };
                    }
                    break;
                case DataType.String:
                    {
                        var stringData = new StringArray();
                        stringData.Data.AddRange(Data as List<string>);

                        fieldData.Scalars = new ScalarField()
                        {
                            StringData = stringData
                        };
                    }
                    break;
                default:
                    break;
            }

            return fieldData;
        }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(FieldName, nameof(FieldName));
            if (Data.IsEmpty())
            {
                throw new ParamException($"{nameof(Field)}.{nameof(Data)} is empty");
            }
        }

        internal void CheckDataType()
        {
            var type = typeof(TData);

            if (type == typeof(bool))
            {
                DataType = DataType.Bool;
            }
            else if (type == typeof(Int16))
            {
                DataType = DataType.Int16;
            }
            else if (type == typeof(int) || type == typeof(Int32))
            {
                DataType = DataType.Int32;
            }
            else if (type == typeof(Int64) || type == typeof(long))
            {
                DataType = DataType.Int64;
            }
            else if (type == typeof(float))
            {
                DataType = DataType.Float;
            }
            else if (type == typeof(double))
            {
                DataType = DataType.Double;
            }
            else if (type == typeof(string))
            {
                DataType = DataType.String;
            }
            else if (type == typeof(List<float>) || type == typeof(FloatArray))
            {
                DataType = DataType.FloatVector;
            }
            else
            {
                throw new NotSupportedException($"Not Support DataType:{DataType}");
            }
        }
    }
}
