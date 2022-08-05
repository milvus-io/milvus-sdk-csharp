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
    public abstract class Field
    {
        public static Field Create<TData>(
            string name,
            List<TData> datas
            )
            where TData : struct
        {
            return new Field<TData>()
            {
                FieldName = name,
                Datas = datas
            };
        }

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

        public static Field CreateBinaryVectors(string name,List<List<float>> datas)
        {
            return new BinaryVectorField()
            {
                FieldName = name,
                Datas = datas,
            };
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
        where TData:struct
    {
        public Field()
        {
            CheckDataType();
        }

        public List<TData> Datas { get; set; }

        public override int RowCount => Datas?.Count ?? 0;

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
                //throw new MilvusException($"DataType Error:{DataType}");
                case DataType.Bool:
                    {
                        var boolData = new BoolArray();
                        boolData.Data.AddRange(Datas as List<bool>);

                        fieldData.Scalars = new ScalarField()
                        {
                            BoolData = boolData
                        };
                    }
                    break;
                case DataType.Int8:
                    throw new NotSupportedException("not support in .net");
                case DataType.Int16:
                    {
                        var intData = new IntArray();
                        intData.Data.AddRange((Datas as List<Int16>).Select(p => (int)p));

                        fieldData.Scalars = new ScalarField()
                        {
                            IntData = intData
                        };
                    }
                    break;
                case DataType.Int32:
                    {
                        var intData = new IntArray();
                        intData.Data.AddRange(Datas as List<int>);

                        fieldData.Scalars = new ScalarField()
                        {
                            IntData = intData
                        };
                    }
                    break;
                case DataType.Int64:
                    {
                        var longData = new LongArray();
                        longData.Data.AddRange(Datas as List<long>);

                        fieldData.Scalars = new ScalarField()
                        {
                            LongData = longData
                        };
                    }
                    break;
                case DataType.Float:
                    {
                        var floatData = new FloatArray();
                        floatData.Data.AddRange(Datas as List<float>);

                        fieldData.Scalars = new ScalarField()
                        {
                            FloatData = floatData
                        };
                    }
                    break;
                case DataType.Double:
                    {
                        var doubleData = new DoubleArray();
                        doubleData.Data.AddRange(Datas as List<double>);

                        fieldData.Scalars = new ScalarField()
                        {
                            DoubleData = doubleData
                        };
                    }
                    break;
                case DataType.String:
                    {
                        var stringData = new StringArray();
                        stringData.Data.AddRange(Datas as List<string>);

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
            if (Datas.IsEmpty())
            {
                throw new ParamException($"{nameof(Field)}.{nameof(Datas)} is empty");
            }
        }

        public void CheckDataType()
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
