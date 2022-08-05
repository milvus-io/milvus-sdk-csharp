using Google.Protobuf;
using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace IO.Milvus.Response
{
    /// <summary>
    /// Utility class to wrap response of <code>query/search</code> interface.
    /// </summary>
    public class FieldDataWrapper
    {
        private FieldData fieldData;

        public FieldDataWrapper(FieldData fieldData)
        {
            this.fieldData = fieldData;
        }

        public bool IsVectorField()
        {
            return fieldData.Type == DataType.FloatVector || fieldData.Type == DataType.BinaryVector;
        }

        /// <summary>
        ///  Gets the dimension value of a vector field.
        /// Throw <see cref="IllegalResponseException"/> if the field is not a vector filed.
        /// </summary>
        public int Dim {
            get
            {
                if (!IsVectorField())
                {
                    throw new IllegalResponseException("Not a vector field");
                }
                return (int)fieldData.Vectors.Dim;
            }
        }

        /// <summary>
        ///  Gets the row count of a field.
        /// Throws <see cref="IllegalResponseException"/> if the field type is illegal.
        /// </summary>
        public long RowCount
        {
            get
            {
                DataType dt = fieldData.Type;
                switch (dt)
                {
                    case DataType.FloatVector:
                        {
                            int dim = Dim;
                            //System.out.println(fieldData.Vectors().FloatVector().DataCount());
                            List<float> data = fieldData.Vectors.FloatVector.Data.ToList();
                            if (data.Count % dim != 0)
                            {
                                throw new IllegalResponseException("Returned float vector field data array size doesn't match dimension");
                            }

                            return data.Count / dim;
                        }
                    case DataType.BinaryVector:
                        {
                            int dim = Dim;
                            ByteString data = fieldData.Vectors.BinaryVector;
                            if (data.Count() % dim != 0)
                            {
                                throw new IllegalResponseException("Returned binary vector field data array size doesn't match dimension");
                            }

                            return data.Count() / dim;
                        }
                    case DataType.Int64:
                        return fieldData.Scalars.LongData.Data.Count;
                    case DataType.Int32:
                    case DataType.Int16:
                    case DataType.Int8:
                        return fieldData.Scalars.IntData.Data.Count;
                    case DataType.Bool:
                        return fieldData.Scalars.BoolData.Data.Count;
                    case DataType.Float:
                        return fieldData.Scalars.FloatData.Data.Count;
                    case DataType.Double:
                        return fieldData.Scalars.DoubleData.Data.Count;
                    //case DataType.VarChar:
                    case DataType.String:
                        return fieldData.Scalars.StringData.Data.Count;
                    default:
                        throw new IllegalResponseException("Unsupported data type returned by FieldData");
                }
            }
        }

        /// <summary>
        ///  Returns the field data according to its type:
        ///  float vector field return List&lt;List&lt;Float&gt;&gt;,
        ///  binary vector field return List&lt;ByteBuffer&gt;,
        ///  int64 field return List&lt;Long&gt;,
        ///  boolean field return List&lt;Boolean&gt;,
        ///  etc.
        /// </summary>
        /// <exception cref="IllegalResponseException"></exception>
        public IList GetFieldData()
        {
            DataType dt = fieldData.Type;
            switch (dt)
            {
                case DataType.FloatVector:
                    {
                        int dim = Dim;
                        //System.out.println(fieldData.getVectors().getFloatVector().getDataCount());
                        List<float> data = fieldData.Vectors.FloatVector.Data.ToList();
                        if (data.Count() % dim != 0)
                        {
                            throw new IllegalResponseException("Returned float vector field data array size doesn't match dimension");
                        }

                        List<List<float>> packData = new List<List<float>>();
                        int count = data.Count() / dim;
                        for (int i = 0; i < count; ++i)
                        {
                            packData.Add(data.GetRange(i * dim, dim));
                        }
                        return packData;
                    }
                case DataType.BinaryVector:
                    {
                        int dim = Dim;
                        ByteString data = fieldData.Vectors.BinaryVector;
                        if (data.Count() % dim != 0)
                        {
                            throw new IllegalResponseException("Returned binary vector field data array size doesn't match dimension");
                        }

                        List<MemoryStream> packData = new List<MemoryStream>();
                        int count = data.Count() / dim;
                        for (int i = 0; i < count; ++i)
                        {
                            var bf = new MemoryStream(dim);
                            bf.Write(data.ToByteArray(), i * dim, dim);                  
                            packData.Add(bf);
                        }
                        return packData;
                    }
                case DataType.Int64:
                    return fieldData.Scalars.LongData.Data.ToList();
                case DataType.Int32:
                case DataType.Int16:
                case DataType.Int8:
                    return fieldData.Scalars.IntData.Data.ToList();
                case DataType.Bool:
                    return fieldData.Scalars.BoolData.Data.ToList();
                case DataType.Float:
                    return fieldData.Scalars.FloatData.Data.ToList();
                case DataType.Double:
                    return fieldData.Scalars.DoubleData.Data.ToList();
                //case VarChar:
                case DataType.String:
                    return fieldData.Scalars.StringData.Data.ToList();
                default:
                    throw new IllegalResponseException("Unsupported data type returned by FieldData");
            }
        }
    }
}
